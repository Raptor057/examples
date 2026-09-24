using System.Data;
using System.Diagnostics;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LabelPrinting.Shared.Persistence;

/// <summary>
/// Envoltura de Dapper con cronometro y registro. Es la UNICA puerta a la base: las clases
/// <c>*Sql</c> de cada modulo la reciben por constructor y no abren conexiones por su cuenta.
///
/// Mide cada consulta y avisa de las lentas. Parece un lujo en un ejemplo, pero es justo lo que
/// hace falta el dia que la pantalla "va lenta" y nadie sabe si la culpa es del SQL o de la red:
/// sin esto se empieza a adivinar.
/// </summary>
public sealed class SqlDbConnection(IConfiguration configuration, ILogger<SqlDbConnection> logger)
{
    /// <summary>A partir de aqui la consulta se registra como lenta.</summary>
    public const int SlowQueryMs = 300;

    private string ConnectionString =>
        configuration.GetConnectionString("LabelPrintingDb")
        ?? throw new InvalidOperationException(
            "Falta la cadena ConnectionStrings:LabelPrintingDb. Ver docs/ARRANQUE-LOCAL.md.");

    public Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        => RunAsync(sql, c => c.QueryAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)));

    public Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        => RunAsync(sql, c => c.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)));

    public Task<int> ExecuteAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        => RunAsync(sql, c => c.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)));

    public Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        => RunAsync(sql, c => c.ExecuteScalarAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken)));

    private async Task<TResult> RunAsync<TResult>(string sql, Func<IDbConnection, Task<TResult>> operation)
    {
        await using var connection = new SqliteConnection(ConnectionString);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await operation(connection).ConfigureAwait(false);
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds >= SlowQueryMs)
            {
                // El SQL se registra recortado: un INSERT de plantillas trae el ZPL entero y
                // llenaria el log de arte ASCII.
                logger.LogWarning("Consulta lenta ({Elapsed} ms): {Sql}", stopwatch.ElapsedMilliseconds, Shorten(sql));
            }

            return result;
        }
        catch (SqliteException ex)
        {
            logger.LogError(ex, "Fallo la consulta: {Sql}", Shorten(sql));
            throw;
        }
    }

    private static string Shorten(string sql)
    {
        var flat = string.Join(' ', sql.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return flat.Length <= 200 ? flat : flat[..200] + "...";
    }
}
