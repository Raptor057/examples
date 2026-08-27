using Automation.Domain.Entities;
using Automation.Domain.Repositories;
using Automation.Domain.Tasks;
using Automation.Infrastructure.Persistence;
using Automation.Infrastructure.Persistence.DbModels.MainDb;
using Automation.Infrastructure.Persistence.Sql.MainDb;
using OpsJobs.Shared.SqlServer;
using OpsJobs.Shared.SqlServer.Markers;

namespace Automation.Infrastructure.Repositories;

/// <summary>Datos de refresh-order-summary.</summary>
internal sealed class OrderSummaryRepository(ConfigurationSqlDbConnection<MainDb> db)
    : IOrderSummaryRepository
{
    public async Task<IReadOnlyList<OrderEventRecord>> GetEventsInWindowAsync(
        TaskWindow window, int maxItems, CancellationToken cancellationToken = default)
    {
        var rows = await db.QueryAsync<OrderEventRow>(
            OrderEventSql.SelectWindow,
            new { FromUtc = window.FromUtc, ToUtc = window.ToUtc, MaxItems = maxItems },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return rows
            .Select(row => new OrderEventRecord(
                row.Id, UtcDates.AsUtc(row.OccurredAtUtc), row.OrderNumber, row.ChannelCode, row.Units, row.Amount))
            .ToList();
    }

    public Task UpsertDailySummaryAsync(
        DateOnly summaryDate,
        string channelCode,
        int orderCount,
        int units,
        decimal amount,
        DateTime refreshedAtUtc,
        CancellationToken cancellationToken = default) =>
        db.ExecuteAsync(
            OrderDailySummarySql.Upsert,
            new
            {
                SummaryDate = summaryDate.ToDateTime(TimeOnly.MinValue),
                ChannelCode = channelCode,
                OrderCount = orderCount,
                Units = units,
                Amount = amount,
                RefreshedAtUtc = refreshedAtUtc
            },
            cancellationToken: cancellationToken);
}

/// <summary>Datos de purge-expired-temp-files.</summary>
internal sealed class TempFileRepository(ConfigurationSqlDbConnection<MainDb> db)
    : ITempFileRepository
{
    public async Task<IReadOnlyList<TempFileRecord>> GetExpiredInWindowAsync(
        TaskWindow window, int maxItems, CancellationToken cancellationToken = default)
    {
        var rows = await db.QueryAsync<TempFileRow>(
            TempFileSql.SelectExpiredInWindow,
            new { FromUtc = window.FromUtc, ToUtc = window.ToUtc, MaxItems = maxItems },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return rows
            .Select(row => new TempFileRecord(row.Id, row.FileName, row.SizeBytes, UtcDates.AsUtc(row.ExpiresAtUtc)))
            .ToList();
    }

    public Task<int> PurgeAsync(long id, DateTime purgedAtUtc, CancellationToken cancellationToken = default) =>
        db.ExecuteAsync(
            TempFileSql.Purge,
            new { Id = id, PurgedAtUtc = purgedAtUtc },
            cancellationToken: cancellationToken);
}

/// <summary>Datos de retry-pending-dispatches.</summary>
internal sealed class PendingDispatchRepository(ConfigurationSqlDbConnection<MainDb> db)
    : IPendingDispatchRepository
{
    public async Task<IReadOnlyList<PendingDispatchRecord>> GetPendingInWindowAsync(
        TaskWindow window, int maxItems, CancellationToken cancellationToken = default)
    {
        var rows = await db.QueryAsync<PendingDispatchRow>(
            PendingDispatchSql.SelectPendingInWindow,
            new { FromUtc = window.FromUtc, ToUtc = window.ToUtc, MaxItems = maxItems },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return rows
            .Select(row => new PendingDispatchRecord(
                row.Id, row.Reference, UtcDates.AsUtc(row.QueuedAtUtc), row.Attempts, row.SimulatedOutcome))
            .ToList();
    }

    public Task MarkSentAsync(long id, int attempts, DateTime attemptedAtUtc, CancellationToken cancellationToken = default) =>
        db.ExecuteAsync(
            PendingDispatchSql.MarkSent,
            new { Id = id, Attempts = attempts, AttemptedAtUtc = attemptedAtUtc },
            cancellationToken: cancellationToken);

    public Task MarkFailedAsync(
        long id, int attempts, string error, DateTime attemptedAtUtc, bool isDead,
        CancellationToken cancellationToken = default) =>
        db.ExecuteAsync(
            PendingDispatchSql.MarkFailed,
            new
            {
                Id = id,
                Attempts = attempts,
                LastError = error.Length <= 400 ? error : error[..400],
                AttemptedAtUtc = attemptedAtUtc,
                Status = isDead ? "DEAD" : "PENDING"
            },
            cancellationToken: cancellationToken);
}
