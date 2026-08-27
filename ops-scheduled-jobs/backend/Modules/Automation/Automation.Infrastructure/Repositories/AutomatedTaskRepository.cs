using Automation.Domain.Entities;
using Automation.Domain.Repositories;
using Automation.Domain.Scheduling;
using Automation.Infrastructure.Persistence;
using Automation.Infrastructure.Persistence.DbModels.MainDb;
using Automation.Infrastructure.Persistence.Sql.MainDb;
using OpsJobs.Shared.SqlServer;
using OpsJobs.Shared.SqlServer.Markers;

namespace Automation.Infrastructure.Repositories;

/// <summary>
/// Ejecuta el SQL de <see cref="AutomatedTaskSql"/> y mapea la fila a la entidad de dominio. No
/// abre conexiones, no arma comandos y no lleva SQL escrito dentro: para eso esta la conexion
/// tipada por marcador que recibe.
/// </summary>
internal sealed class AutomatedTaskRepository(ConfigurationSqlDbConnection<MainDb> db)
    : IAutomatedTaskRepository
{
    public async Task<IReadOnlyList<ClaimedTask>> ClaimDueAsync(
        IReadOnlyCollection<string> knownCodes,
        DateTime nowUtc,
        DateTime staleClaimBeforeUtc,
        string claimedBy,
        CancellationToken cancellationToken = default)
    {
        // Sin codigos conocidos no hay nada que reclamar. Importa mas de lo que parece: Dapper
        // expande una lista vacia a "IN (NULL)", que no falla y no devuelve nada, pero cortar
        // aqui deja explicito que un catalogo vacio significa "ninguna tarea", no "todas".
        if (knownCodes.Count == 0) return [];

        var rows = await db.QueryAsync<ClaimedTaskRow>(
            AutomatedTaskSql.ClaimDue,
            new
            {
                NowUtc = nowUtc,
                ClaimedBy = claimedBy,
                StaleClaimBeforeUtc = staleClaimBeforeUtc,
                KnownCodes = knownCodes
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return rows.Select(ToClaimedTask).ToList();
    }

    public async Task<ClaimedTask?> ClaimSpecificAsync(
        string code,
        DateTime nowUtc,
        DateTime staleClaimBeforeUtc,
        string claimedBy,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.QueryAsync<ClaimedTaskRow>(
            AutomatedTaskSql.ClaimSpecific,
            new
            {
                Code = code,
                NowUtc = nowUtc,
                ClaimedBy = claimedBy,
                StaleClaimBeforeUtc = staleClaimBeforeUtc
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var row = rows.FirstOrDefault();
        return row is null ? null : ToClaimedTask(row);
    }

    public Task CompleteRunAsync(
        string code,
        DateTime lastRunAtUtc,
        DateTime nextRunAtUtc,
        DateTime lastCutoffUtc,
        string lastStatus,
        CancellationToken cancellationToken = default) =>
        db.ExecuteAsync(
            AutomatedTaskSql.CompleteRun,
            new
            {
                Code = code,
                LastRunAtUtc = lastRunAtUtc,
                NextRunAtUtc = nextRunAtUtc,
                LastCutoffUtc = lastCutoffUtc,
                LastStatus = lastStatus,
                NowUtc = DateTime.UtcNow
            },
            cancellationToken: cancellationToken);

    public Task ReleaseClaimAsync(string code, CancellationToken cancellationToken = default) =>
        db.ExecuteAsync(
            AutomatedTaskSql.ReleaseClaim,
            new { Code = code, NowUtc = DateTime.UtcNow },
            cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<AutomatedTaskState>> ListAsync(
        IReadOnlyCollection<string> knownCodes, CancellationToken cancellationToken = default)
    {
        if (knownCodes.Count == 0) return [];

        var rows = await db.QueryAsync<AutomatedTaskRow>(
            AutomatedTaskSql.SelectKnown,
            new { KnownCodes = knownCodes },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return rows.Select(ToState).ToList();
    }

    public async Task<AutomatedTaskState?> FindAsync(string code, CancellationToken cancellationToken = default)
    {
        var row = await db.QuerySingleAsync<AutomatedTaskRow>(
            AutomatedTaskSql.SelectByCode,
            new { Code = code },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return row is null ? null : ToState(row);
    }

    public async Task<bool> SetEnabledAsync(
        string code,
        bool isEnabled,
        string? pausedReason,
        DateTime? nextRunAtUtc,
        CancellationToken cancellationToken = default)
    {
        var affected = await db.ExecuteAsync(
            AutomatedTaskSql.SetEnabled,
            new
            {
                Code = code,
                IsEnabled = isEnabled,
                PausedReason = pausedReason,
                NextRunAtUtc = nextRunAtUtc,
                NowUtc = DateTime.UtcNow
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return affected > 0;
    }

    public async Task<int> SeedMissingAsync(
        IReadOnlyList<AutomatedTaskSeed> seeds, CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var inserted = 0;

        foreach (var seed in seeds)
        {
            inserted += await db.ExecuteAsync(
                AutomatedTaskSql.InsertMissing,
                new
                {
                    seed.Code,
                    seed.ScheduleKind,
                    seed.IntervalMinutes,
                    seed.RunAtLocalTimes,
                    seed.NextRunAtUtc,
                    seed.LastCutoffUtc,
                    NowUtc = nowUtc
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return inserted;
    }

    private static ClaimedTask ToClaimedTask(ClaimedTaskRow row) => new(
        new AutomatedTaskState(
            row.Code,
            row.IsEnabled,
            row.PausedReason,
            TaskSchedule.FromStorage(row.ScheduleKind, row.IntervalMinutes, row.RunAtLocalTimes),
            UtcDates.AsUtc(row.LastRunAtUtc),
            UtcDates.AsUtc(row.NextRunAtUtc),
            row.LastStatus,
            UtcDates.AsUtc(row.LastCutoffUtc),
            UtcDates.AsUtc(row.ClaimedAtUtc),
            row.ClaimedBy),
        UtcDates.AsUtc(row.PreviousClaimedAtUtc),
        row.PreviousClaimedBy);

    private static AutomatedTaskState ToState(AutomatedTaskRow row) => new(
        row.Code,
        row.IsEnabled,
        row.PausedReason,
        TaskSchedule.FromStorage(row.ScheduleKind, row.IntervalMinutes, row.RunAtLocalTimes),
        UtcDates.AsUtc(row.LastRunAtUtc),
        UtcDates.AsUtc(row.NextRunAtUtc),
        row.LastStatus,
        UtcDates.AsUtc(row.LastCutoffUtc),
        UtcDates.AsUtc(row.ClaimedAtUtc),
        row.ClaimedBy);
}
