using Automation.Domain.Entities;

namespace Automation.Application.Dtos;

/// <summary>
/// Entidad de dominio a DTO, en un solo lugar. Duplicar este mapeo por caso de uso es como se
/// llega a que la lista y el detalle muestren campos distintos de la misma corrida.
/// </summary>
internal static class AutomationMapper
{
    public static TaskRunDto ToDto(AutomatedTaskRun run) => new(
        run.Id,
        run.TaskCode,
        run.StartedAtUtc,
        run.FinishedAtUtc,
        run.Status,
        run.ItemsProcessed,
        run.ItemsFailed,
        run.Message,
        run.WindowFromUtc,
        run.WindowToUtc,
        run.CursorBeforeUtc,
        run.CursorAfterUtc,
        run.TriggeredByUser,
        run.DispatcherInstance);

    /// <summary>
    /// Una tarea reclamada esta CORRIENDO solo si su reclamo sigue vigente. Un reclamo vencido
    /// no es "corriendo": es una corrida que se murio, y pintarla como activa esconde justo lo
    /// que hay que ver.
    /// </summary>
    public static AutomatedTaskDto ToDto(AutomatedTaskState state, AutomatedTaskRun? lastRun, DateTime staleClaimBeforeUtc) => new(
        state.Code,
        state.IsEnabled,
        state.PausedReason,
        state.Schedule.StorageKind,
        state.Schedule.IntervalMinutes,
        state.Schedule.RunAtLocalTimes,
        state.LastRunAtUtc,
        state.NextRunAtUtc,
        state.LastStatus,
        state.LastCutoffUtc,
        state.ClaimedAtUtc,
        state.ClaimedBy,
        state.ClaimedAtUtc is not null && state.ClaimedAtUtc > staleClaimBeforeUtc,
        lastRun is null ? null : ToDto(lastRun));
}
