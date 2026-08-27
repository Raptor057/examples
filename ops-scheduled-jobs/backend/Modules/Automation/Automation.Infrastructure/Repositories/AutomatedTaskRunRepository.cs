using Automation.Domain.Entities;
using Automation.Domain.Repositories;
using Automation.Infrastructure.Persistence;
using Automation.Infrastructure.Persistence.DbModels.MainDb;
using Automation.Infrastructure.Persistence.Sql.MainDb;
using OpsJobs.Shared.SqlServer;
using OpsJobs.Shared.SqlServer.Markers;

namespace Automation.Infrastructure.Repositories;

internal sealed class AutomatedTaskRunRepository(ConfigurationSqlDbConnection<MainDb> db)
    : IAutomatedTaskRunRepository
{
    public async Task<long> StartAsync(AutomatedTaskRunStart start, CancellationToken cancellationToken = default)
    {
        var id = await db.ExecuteScalarAsync<long>(
            AutomatedTaskRunSql.Insert,
            new
            {
                start.TaskCode,
                start.StartedAtUtc,
                start.WindowFromUtc,
                start.WindowToUtc,
                start.CursorBeforeUtc,
                start.TriggeredByUser,
                start.DispatcherInstance,
                NowUtc = start.StartedAtUtc
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return id;
    }

    public Task FinishAsync(
        long runId,
        string status,
        int itemsProcessed,
        int itemsFailed,
        string message,
        DateTime finishedAtUtc,
        DateTime? cursorAfterUtc,
        CancellationToken cancellationToken = default) =>
        db.ExecuteAsync(
            AutomatedTaskRunSql.Finish,
            new
            {
                Id = runId,
                Status = status,
                ItemsProcessed = itemsProcessed,
                ItemsFailed = itemsFailed,
                // El mensaje se recorta al tamano de la columna. Un mensaje largo -una excepcion
                // con su cadena de causas- haria fallar el UPDATE, y perder la bitacora por no
                // poder escribir el detalle del fallo es el peor momento posible para perderla.
                Message = Truncate(message, 1000),
                FinishedAtUtc = finishedAtUtc,
                CursorAfterUtc = cursorAfterUtc
            },
            cancellationToken: cancellationToken);

    public async Task<int> AbandonOpenRunsAsync(
        string taskCode, DateTime nowUtc, string message, CancellationToken cancellationToken = default)
    {
        return await db.ExecuteAsync(
            AutomatedTaskRunSql.AbandonOpen,
            new { TaskCode = taskCode, NowUtc = nowUtc, Message = Truncate(message, 1000) },
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<AutomatedTaskRun>> GetPageAsync(
        string taskCode, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        // El desplazamiento se calcula aqui y viaja como PARAMETRO. Calcularlo dentro del SQL
        // obligaria a interpolarlo, que es justo lo que no se hace nunca.
        var offset = (pageNumber - 1) * pageSize;

        var rows = (await db.QueryAsync<AutomatedTaskRunRow>(
            AutomatedTaskRunSql.SelectPage,
            new { TaskCode = taskCode, Offset = offset, PageSize = pageSize },
            cancellationToken: cancellationToken).ConfigureAwait(false)).ToList();

        // Con la pagina vacia no hay ninguna fila de la que leer el COUNT OVER(). Cero es la
        // respuesta correcta: si el total fuera mayor, habria filas.
        var totalCount = rows.Count == 0 ? 0 : rows[0].TotalCount;

        return new PagedResult<AutomatedTaskRun>(
            rows.Select(ToRun).ToList(), totalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyList<AutomatedTaskRun>> GetLatestPerTaskAsync(
        IReadOnlyCollection<string> taskCodes, CancellationToken cancellationToken = default)
    {
        if (taskCodes.Count == 0) return [];

        var rows = await db.QueryAsync<AutomatedTaskRunRow>(
            AutomatedTaskRunSql.SelectLatestPerTask,
            new { TaskCodes = taskCodes },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return rows.Select(ToRun).ToList();
    }

    private static AutomatedTaskRun ToRun(AutomatedTaskRunRow row) => new(
        row.Id,
        row.TaskCode,
        UtcDates.AsUtc(row.StartedAtUtc),
        UtcDates.AsUtc(row.FinishedAtUtc),
        row.Status,
        row.ItemsProcessed,
        row.ItemsFailed,
        row.Message,
        UtcDates.AsUtc(row.WindowFromUtc),
        UtcDates.AsUtc(row.WindowToUtc),
        UtcDates.AsUtc(row.CursorBeforeUtc),
        UtcDates.AsUtc(row.CursorAfterUtc),
        row.TriggeredByUser,
        row.DispatcherInstance);

    private static string Truncate(string value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}
