using Automation.Domain.Scheduling;

namespace Automation.Domain.Entities;

/// <summary>
/// La fila de configuracion y estado de una tarea, ya reconstruida como objeto de dominio.
///
/// Es la mitad mutable del patron: el catalogo en codigo dice que la tarea existe, y esto dice
/// como esta puesta hoy.
/// </summary>
public sealed record AutomatedTaskState(
    string Code,
    bool IsEnabled,
    string? PausedReason,
    TaskSchedule Schedule,
    DateTime? LastRunAtUtc,
    DateTime NextRunAtUtc,
    string? LastStatus,
    DateTime LastCutoffUtc,
    DateTime? ClaimedAtUtc,
    string? ClaimedBy);

/// <summary>
/// Una tarea que ESTA instancia acaba de ganar, con el reclamo anterior que se piso.
///
/// <see cref="PreviousClaimedAtUtc"/> no es curiosidad: si trae valor, este reclamo era uno
/// VENCIDO que se retomo, y entonces hay una corrida anterior que quedo abierta y que nadie va
/// a cerrar. El despachador la cierra al empezar la suya.
/// </summary>
public sealed record ClaimedTask(
    AutomatedTaskState State,
    DateTime? PreviousClaimedAtUtc,
    string? PreviousClaimedBy)
{
    public bool TookOverStaleClaim => PreviousClaimedAtUtc is not null;
}

/// <summary>Valores con los que nace una tarea que el catalogo declara y la base todavia no tiene.</summary>
public sealed record AutomatedTaskSeed(
    string Code,
    string ScheduleKind,
    int? IntervalMinutes,
    string? RunAtLocalTimes,
    DateTime NextRunAtUtc,
    DateTime LastCutoffUtc);

/// <summary>Una pagina de resultados con su total REAL, el que calcula el motor.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize)
{
    public bool HasMore => (long)PageNumber * PageSize < TotalCount;
}
