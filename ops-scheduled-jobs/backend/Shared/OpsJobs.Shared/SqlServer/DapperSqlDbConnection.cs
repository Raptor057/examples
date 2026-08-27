using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OpsJobs.Shared.SqlServer;

/// <summary>
/// Unico lugar del proyecto que habla con Dapper. Centraliza timing, logging estructurado,
/// tiers de slow-query y hash del SQL: ningun repositorio abre conexiones ni arma comandos.
///
/// Aqui el acceso a datos es Dapper de punta a punta, no un escape puntual. La regla de oro 2
/// del catalogo lo decide sola: este proyecto NO tiene tenancy, asi que no hay filtro de
/// aislamiento que olvidar, y entonces no se paga el costo del ORM.
/// </summary>
public class DapperSqlDbConnection(
    SqlDbConnectionFactory connections,
    ILogger<DapperSqlDbConnection> logger,
    IConfiguration configuration)
{
    private readonly bool _includeSqlText = configuration.GetValue<bool>("CustomLogging:IncludeSqlText");

    public async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? param = null,
        string? queryName = null,
        LogLevel level = LogLevel.Debug,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "")
    {
        var resolved = ResolveQueryName(queryName, callerMemberName, callerFilePath, nameof(QueryAsync));
        return await ExecuteTimedAsync(resolved, sql, async () =>
        {
            using var connection = await connections.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var command = new CommandDefinition(sql, param, commandTimeout: commandTimeout, cancellationToken: cancellationToken);
            return await connection.QueryAsync<T>(command).ConfigureAwait(false);
        }, param, level).ConfigureAwait(false);
    }

    public async Task<T?> QuerySingleAsync<T>(
        string sql,
        object? param = null,
        string? queryName = null,
        LogLevel level = LogLevel.Debug,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "")
    {
        var resolved = ResolveQueryName(queryName, callerMemberName, callerFilePath, nameof(QuerySingleAsync));
        return await ExecuteTimedAsync(resolved, sql, async () =>
        {
            using var connection = await connections.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var command = new CommandDefinition(sql, param, commandTimeout: commandTimeout, cancellationToken: cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<T>(command).ConfigureAwait(false);
        }, param, level).ConfigureAwait(false);
    }

    public async Task<int> ExecuteAsync(
        string sql,
        object? param = null,
        string? queryName = null,
        LogLevel level = LogLevel.Debug,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "")
    {
        var resolved = ResolveQueryName(queryName, callerMemberName, callerFilePath, nameof(ExecuteAsync));
        return await ExecuteTimedAsync(resolved, sql, async () =>
        {
            using var connection = await connections.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var command = new CommandDefinition(sql, param, commandTimeout: commandTimeout, cancellationToken: cancellationToken);
            return await connection.ExecuteAsync(command).ConfigureAwait(false);
        }, param, level).ConfigureAwait(false);
    }

    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? param = null,
        string? queryName = null,
        LogLevel level = LogLevel.Debug,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "")
    {
        var resolved = ResolveQueryName(queryName, callerMemberName, callerFilePath, nameof(ExecuteScalarAsync));
        return await ExecuteTimedAsync(resolved, sql, async () =>
        {
            using var connection = await connections.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var command = new CommandDefinition(sql, param, commandTimeout: commandTimeout, cancellationToken: cancellationToken);
            return await connection.ExecuteScalarAsync<T>(command).ConfigureAwait(false);
        }, param, level).ConfigureAwait(false);
    }

    /// <summary>
    /// Transaccion multi-statement: UNA conexion y UNA transaccion compartidas. El wrapper abre
    /// una conexion por llamada, asi que simular una transaccion con varias llamadas sueltas NO
    /// es atomico: cada una abriria la suya.
    /// </summary>
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<IDbConnection, IDbTransaction, Task<T>> work,
        IsolationLevel isolation = IsolationLevel.ReadCommitted,
        string? queryName = null,
        LogLevel level = LogLevel.Debug,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "")
    {
        var resolved = ResolveQueryName(queryName, callerMemberName, callerFilePath, "Transaction");
        return await ExecuteTimedAsync(resolved, "-- transaction --", async () =>
        {
            using var connection = await connections.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            using var transaction = connection.BeginTransaction(isolation);
            try
            {
                var result = await work(connection, transaction).ConfigureAwait(false);
                transaction.Commit();
                return result;
            }
            catch
            {
                transaction.Rollback();
                // Se re-lanza: el fallo de negocio se modela como Response tipado en el handler,
                // no se traga aqui.
                throw;
            }
        }, null, level).ConfigureAwait(false);
    }

    // Nucleo de observabilidad: mide, clasifica la consulta lenta y loguea. Nunca traga la
    // excepcion.
    private async Task<T> ExecuteTimedAsync<T>(string queryName, string sql, Func<Task<T>> action, object? param, LogLevel level)
    {
        var stopwatch = Stopwatch.StartNew();
        var sqlHash = ComputeSqlHash(sql);
        try
        {
            var result = await action().ConfigureAwait(false);
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;
            var outcomeLevel = elapsed >= 2000 ? LogLevel.Critical
                : elapsed >= 1000 ? LogLevel.Error
                : elapsed >= 300 ? LogLevel.Warning
                : level;
            LogSql(outcomeLevel, queryName, elapsed, sqlHash, sql, param, elapsed >= 300 ? "SLOW" : "OK");
            return result;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            LogSql(LogLevel.Error, queryName, stopwatch.ElapsedMilliseconds, sqlHash, sql, param, "FAIL", exception);
            throw;
        }
    }

    private void LogSql(LogLevel level, string queryName, long elapsedMs, string sqlHash, string sqlText, object? param, string outcome, Exception? exception = null)
    {
        // El texto del SQL sale bajo bandera de configuracion: los parametros pueden llevar
        // datos sensibles y que aparezcan en el log tiene que ser una decision, no un descuido.
        const string withSql = "SQL {QueryName} {Outcome} in {ElapsedMs} ms | hash: {SqlHash} | sql: {SqlText} | params: {@Params}";
        const string withoutSql = "SQL {QueryName} {Outcome} in {ElapsedMs} ms | hash: {SqlHash} | params: {@Params}";
        if (_includeSqlText)
            logger.Log(level, exception, withSql, queryName, outcome, elapsedMs, sqlHash, sqlText, param);
        else
            logger.Log(level, exception, withoutSql, queryName, outcome, elapsedMs, sqlHash, param);
    }

    private static string ResolveQueryName(string? queryName, string caller, string callerFile, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(queryName)) return queryName;
        var className = string.IsNullOrWhiteSpace(callerFile) ? string.Empty : Path.GetFileNameWithoutExtension(callerFile);
        if (!string.IsNullOrWhiteSpace(className) && !string.IsNullOrWhiteSpace(caller)) return $"{className}.{caller}";
        return string.IsNullOrWhiteSpace(caller) ? fallback : caller;
    }

    private static string ComputeSqlHash(string sql) =>
        string.IsNullOrWhiteSpace(sql)
            ? "NA"
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql)))[..16];
}

/// <summary>Conexion tipada por marcador: es la que inyecta cada repositorio.</summary>
public class ConfigurationSqlDbConnection<TMarker>(
    ConfigurationSqlDbConnectionFactory<TMarker> factory,
    ILogger<DapperSqlDbConnection> logger,
    IConfiguration configuration)
    : DapperSqlDbConnection(factory, logger, configuration);
