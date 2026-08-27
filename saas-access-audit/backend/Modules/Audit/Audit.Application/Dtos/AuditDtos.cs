namespace Audit.Application.Dtos;

/// <summary>
/// Un renglon de la bitacora de acciones, tal como viaja al cliente.
///
/// Ojo con lo que NO trae: texto compuesto. El servidor manda DATOS -codigo de accion, llave,
/// fecha UTC, banderas- y el cliente arma la frase en su idioma y con sus formatos. Componerla
/// aqui haria imposible cumplir i18n en el frontend, y ademas dejaria la bitacora en el idioma
/// del servidor para siempre.
/// </summary>
public sealed record ActionAuditItemDto(
    Guid PublicId,
    string ActionCode,
    string SubjectType,
    string SubjectKey,
    string SubjectLabel,
    string Reason,
    string PerformedBy,
    string PerformedByDisplay,
    string? AuthorizedBy,
    DateTime OccurredAtUtc,
    string? ExternalEffectName,
    bool? ExternalEffectApplied,
    string? ExternalEffectError);

public sealed record ActionAuditPageDto(
    IReadOnlyList<ActionAuditItemDto> Items,
    long TotalCount,
    int PageNumber,
    int PageSize);

public sealed record AccessDenialItemDto(
    Guid PublicId,
    string PermissionCode,
    string Route,
    string HttpMethod,
    string AttemptedBy,
    string AttemptedByDisplay,
    bool EnforcementEnabled,
    bool Blocked,
    DateTime OccurredAtUtc);

public sealed record AccessDenialPageDto(
    IReadOnlyList<AccessDenialItemDto> Items,
    long TotalCount,
    int PageNumber,
    int PageSize);
