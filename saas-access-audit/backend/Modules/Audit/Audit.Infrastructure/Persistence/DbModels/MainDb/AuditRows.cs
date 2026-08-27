namespace Audit.Infrastructure.Persistence.DbModels.MainDb;

/// <summary>
/// Fila cruda de la bitacora de acciones tal como la devuelve el motor.
///
/// Existe separada del modelo de dominio por una razon concreta: trae TotalCount, que no es un
/// dato de la accion sino del conjunto filtrado. El COUNT(1) OVER() lo repite en cada fila, y esa
/// rareza se queda aqui, en la frontera, en vez de contaminar el modelo que ve la aplicacion.
/// </summary>
internal sealed class ActionAuditRow
{
    public Guid PublicId { get; init; }
    public string ActionCode { get; init; } = string.Empty;
    public string SubjectType { get; init; } = string.Empty;
    public string SubjectKey { get; init; } = string.Empty;
    public string SubjectLabel { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string PerformedBy { get; init; } = string.Empty;
    public string PerformedByDisplay { get; init; } = string.Empty;
    public string? AuthorizedBy { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public string? ExternalEffectName { get; init; }
    public bool? ExternalEffectApplied { get; init; }
    public string? ExternalEffectError { get; init; }
    public long TotalCount { get; init; }
}

internal sealed class AccessDenialRow
{
    public Guid PublicId { get; init; }
    public string PermissionCode { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public string HttpMethod { get; init; } = string.Empty;
    public string AttemptedBy { get; init; } = string.Empty;
    public string AttemptedByDisplay { get; init; } = string.Empty;
    public bool EnforcementEnabled { get; init; }
    public bool Blocked { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public long TotalCount { get; init; }
}
