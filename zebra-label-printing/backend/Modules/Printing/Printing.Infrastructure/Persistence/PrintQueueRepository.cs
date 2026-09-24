using System.Globalization;
using LabelPrinting.Shared.Persistence;
using Printing.Domain.Abstractions;
using Printing.Domain.Entities;

namespace Printing.Infrastructure.Persistence;

internal sealed class PrintQueueRepository(SqlDbConnection connection) : IPrintQueue
{
    public async Task<long> EnqueueAsync(PrintQueueItem item, CancellationToken cancellationToken)
        => await connection.ExecuteScalarAsync<long>(PrintQueueSql.Insert, new
        {
            item.TargetJson,
            item.TargetLabel,
            item.TemplateCode,
            item.Dpi,
            item.Zpl,
            item.Attempts,
            NextAttemptAtUtc = Iso(item.NextAttemptAtUtc),
            item.LastError,
            CreatedAtUtc = Iso(item.CreatedAtUtc),
        }, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<PrintQueueItem>> ClaimDueAsync(int max, DateTime now, CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync<PrintQueueRow>(
            PrintQueueSql.ClaimDue, new { Max = max, Now = Iso(now) }, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => r.ToItem())];
    }

    public Task MarkSentAsync(long id, DateTime now, CancellationToken cancellationToken)
        => connection.ExecuteAsync(PrintQueueSql.MarkSent, new { Id = id, Now = Iso(now) }, cancellationToken);

    public Task MarkFailedAsync(long id, int attempts, string error, DateTime nextAttemptAtUtc, bool dead, CancellationToken cancellationToken)
        => connection.ExecuteAsync(PrintQueueSql.MarkFailed, new
        {
            Id = id,
            Attempts = attempts,
            Error = error,
            NextAttemptAtUtc = Iso(nextAttemptAtUtc),
            Dead = dead ? 1 : 0,
        }, cancellationToken);

    public async Task<bool> CancelAsync(long id, DateTime now, CancellationToken cancellationToken)
        => await connection.ExecuteAsync(PrintQueueSql.Cancel, new { Id = id, Now = Iso(now) }, cancellationToken)
            .ConfigureAwait(false) > 0;

    public async Task<IReadOnlyList<PrintQueueItem>> ListAsync(PrintQueueStatus? status, int take, CancellationToken cancellationToken)
    {
        var rows = await connection.QueryAsync<PrintQueueRow>(
            PrintQueueSql.List, new { Status = status is null ? (int?)null : (int)status.Value, Take = take },
            cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => r.ToItem())];
    }

    public async Task<IReadOnlyDictionary<PrintQueueStatus, int>> CountByStatusAsync(CancellationToken cancellationToken)
    {
        var rows = await connection
            .QueryAsync<StatusCountRow>(PrintQueueSql.CountByStatus, null, cancellationToken)
            .ConfigureAwait(false);
        return rows.ToDictionary(r => (PrintQueueStatus)r.Status, r => (int)r.Total);
    }

    /// <summary>
    /// Las fechas viajan a SQLite como texto ISO-8601 en UTC, con la "O" de round-trip. Es lo unico
    /// que garantiza que la comparacion `NextAttemptAtUtc &lt;= @Now` sea correcta: SQLite compara
    /// texto, asi que el formato TIENE que ser ordenable alfabeticamente.
    /// </summary>
    private static string Iso(DateTime value)
        => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private sealed class StatusCountRow
    {
        public long Status { get; init; }
        public long Total { get; init; }
    }
}
