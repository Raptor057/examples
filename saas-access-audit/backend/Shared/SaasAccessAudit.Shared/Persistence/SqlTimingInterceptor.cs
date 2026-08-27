using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SaasAccessAudit.Shared.Persistence;

/// <summary>
/// El equivalente EF del wrapper de Dapper: mismos tiers de consulta lenta y mismo hash del SQL,
/// pero desde un solo punto que ve TODAS las consultas, incluidas las que genera LINQ.
/// </summary>
public sealed class SqlTimingInterceptor(ILogger<SqlTimingInterceptor> logger, IConfiguration configuration)
    : DbCommandInterceptor
{
    private readonly bool _includeSqlText = configuration.GetValue<bool>("CustomLogging:IncludeSqlText");

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        Log(command, eventData.Duration, "OK");
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Log(command, eventData.Duration, "OK");
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        Log(command, eventData.Duration, "OK");
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        Log(command, eventData.Duration, "FAIL", eventData.Exception);
        base.CommandFailed(command, eventData);
    }

    private void Log(DbCommand command, TimeSpan elapsed, string outcome, Exception? exception = null)
    {
        var elapsedMs = (long)elapsed.TotalMilliseconds;
        var level = outcome == "FAIL" ? LogLevel.Error
            : elapsedMs >= 2000 ? LogLevel.Critical
            : elapsedMs >= 1000 ? LogLevel.Error
            : elapsedMs >= 300 ? LogLevel.Warning
            : LogLevel.Debug;
        // El hash cuesta: se calcula SOLO si el nivel esta habilitado. Sin esta guarda, una
        // siembra de dos millones de filas paga un SHA256 por sentencia para tirarlo a la basura.
        if (!logger.IsEnabled(level)) return;

        var tag = outcome == "OK" && elapsedMs >= 300 ? "SLOW" : outcome;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(command.CommandText)))[..16];

        if (_includeSqlText)
            logger.Log(level, exception, "EFSQL {Outcome} in {ElapsedMs} ms | hash: {SqlHash} | sql: {Sql}", tag, elapsedMs, hash, command.CommandText);
        else
            logger.Log(level, exception, "EFSQL {Outcome} in {ElapsedMs} ms | hash: {SqlHash}", tag, elapsedMs, hash);
    }
}
