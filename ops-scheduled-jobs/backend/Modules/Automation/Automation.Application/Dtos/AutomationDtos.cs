namespace Automation.Application.Dtos;

/// <summary>
/// Una corrida, tal y como la ve la pantalla.
///
/// Todas las fechas viajan en UTC y sin formatear. El servidor manda DATO, no texto compuesto:
/// la hora local de quien mira y el idioma de "hace 3 minutos" los pone el cliente, y por eso
/// la misma respuesta sirve en espanol y en ingles sin tocar el backend.
/// </summary>
public sealed record TaskRunDto(
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

/// <summary>Una tarea con su configuracion, su estado y como le fue la ultima vez.</summary>
public sealed record AutomatedTaskDto(
    string Code,
    bool IsEnabled,
    string? PausedReason,
    string ScheduleKind,
    int? IntervalMinutes,
    string? RunAtLocalTimes,
    DateTime? LastRunAtUtc,
    DateTime NextRunAtUtc,
    string? LastStatus,
    DateTime LastCutoffUtc,
    DateTime? ClaimedAtUtc,
    string? ClaimedBy,
    bool IsRunning,
    TaskRunDto? LastRun);

/// <summary>
/// La lista completa. Lleva la zona horaria de programacion porque el horario esta capturado en
/// hora LOCAL y sin saber cual, la pantalla no puede explicar "02:00" sin mentir.
/// </summary>
public sealed record AutomatedTaskListDto(
    IReadOnlyList<AutomatedTaskDto> Items,
    string SchedulingTimeZoneId,
    bool CanAdminister);

/// <summary>Pagina de la bitacora. El total es el REAL, no el de la pagina.</summary>
public sealed record TaskRunPageDto(
    string TaskCode,
    IReadOnlyList<TaskRunDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    bool HasMore);

/// <summary>Resultado de pausar o reanudar.</summary>
public sealed record TaskEnabledDto(string Code, bool IsEnabled, string? PausedReason, DateTime NextRunAtUtc);

/// <summary>Resultado de "ejecutar ahora": lo que de verdad paso, no un "listo".</summary>
public sealed record TaskRunNowDto(
    string Code,
    string Status,
    int ItemsProcessed,
    int ItemsFailed,
    string Message,
    DateTime WindowFromUtc,
    DateTime WindowToUtc,
    DateTime CursorAfterUtc);
