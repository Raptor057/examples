namespace Automation.Domain.Entities;

/// <summary>
/// Una linea de la bitacora. Append-only: se inserta al empezar y se cierra con su resultado;
/// nunca se borra ni se reescribe una corrida vieja.
///
/// <see cref="WindowFromUtc"/> y <see cref="WindowToUtc"/> son las que permiten responder "y lo
/// del martes, quien lo proceso" sin adivinar. <see cref="CursorAfterUtc"/> por debajo de
/// <see cref="WindowToUtc"/> es la senal visible de que algo quedo pendiente a proposito.
/// </summary>
public sealed record AutomatedTaskRun(
    long Id,
    string TaskCode,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc,
    string Status,
    int ItemsProcessed,
    int ItemsFailed,
    string? Message,
    DateTime WindowFromUtc,
    DateTime WindowToUtc,
    DateTime CursorBeforeUtc,
    DateTime? CursorAfterUtc,
    string? TriggeredByUser,
    string? DispatcherInstance);

/// <summary>Lo que se sabe de una corrida en el momento de abrirla.</summary>
public sealed record AutomatedTaskRunStart(
    string TaskCode,
    DateTime StartedAtUtc,
    DateTime WindowFromUtc,
    DateTime WindowToUtc,
    DateTime CursorBeforeUtc,
    string? TriggeredByUser,
    string DispatcherInstance);
