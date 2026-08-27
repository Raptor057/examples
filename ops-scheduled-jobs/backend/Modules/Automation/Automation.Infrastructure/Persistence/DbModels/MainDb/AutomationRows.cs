namespace Automation.Infrastructure.Persistence.DbModels.MainDb;

/// <summary>
/// Los Row son la forma de la FILA, no la del dominio. Existen para que el mapeo de Dapper sea
/// directo y para que un cambio de columna se note aqui y no se cuele hasta el dominio.
///
/// En SQL Server el nombre de la columna y el de la propiedad coinciden tal cual (PascalCase en
/// las dos orillas), asi que no hace falta ningun puente de nomenclatura.
/// </summary>
internal sealed class AutomatedTaskRow
{
    public string Code { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public string? PausedReason { get; init; }
    public string ScheduleKind { get; init; } = string.Empty;
    public int? IntervalMinutes { get; init; }
    public string? RunAtLocalTimes { get; init; }
    public DateTime? LastRunAtUtc { get; init; }
    public DateTime NextRunAtUtc { get; init; }
    public string? LastStatus { get; init; }
    public DateTime LastCutoffUtc { get; init; }
    public DateTime? ClaimedAtUtc { get; init; }
    public string? ClaimedBy { get; init; }
}

/// <summary>La fila que devuelve el OUTPUT del reclamo: el estado nuevo mas el reclamo que se piso.</summary>
internal sealed class ClaimedTaskRow
{
    public string Code { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public string? PausedReason { get; init; }
    public string ScheduleKind { get; init; } = string.Empty;
    public int? IntervalMinutes { get; init; }
    public string? RunAtLocalTimes { get; init; }
    public DateTime? LastRunAtUtc { get; init; }
    public DateTime NextRunAtUtc { get; init; }
    public string? LastStatus { get; init; }
    public DateTime LastCutoffUtc { get; init; }
    public DateTime? ClaimedAtUtc { get; init; }
    public string? ClaimedBy { get; init; }
    public DateTime? PreviousClaimedAtUtc { get; init; }
    public string? PreviousClaimedBy { get; init; }
}

internal sealed class AutomatedTaskRunRow
{
    public long Id { get; init; }
    public string TaskCode { get; init; } = string.Empty;
    public DateTime StartedAtUtc { get; init; }
    public DateTime? FinishedAtUtc { get; init; }
    public string Status { get; init; } = string.Empty;
    public int ItemsProcessed { get; init; }
    public int ItemsFailed { get; init; }
    public string? Message { get; init; }
    public DateTime WindowFromUtc { get; init; }
    public DateTime WindowToUtc { get; init; }
    public DateTime CursorBeforeUtc { get; init; }
    public DateTime? CursorAfterUtc { get; init; }
    public string? TriggeredByUser { get; init; }
    public string? DispatcherInstance { get; init; }

    /// <summary>Lo calcula COUNT(1) OVER(): el total REAL, no el de la pagina.</summary>
    public int TotalCount { get; init; }
}

internal sealed class OrderEventRow
{
    public long Id { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string ChannelCode { get; init; } = string.Empty;
    public int Units { get; init; }
    public decimal Amount { get; init; }
}

internal sealed class TempFileRow
{
    public long Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
}

internal sealed class PendingDispatchRow
{
    public long Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public DateTime QueuedAtUtc { get; init; }
    public int Attempts { get; init; }
    public string SimulatedOutcome { get; init; } = string.Empty;
}
