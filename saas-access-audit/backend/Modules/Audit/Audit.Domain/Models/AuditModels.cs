namespace Audit.Domain.Models;

/// <summary>
/// Filtros de la pantalla de acciones. Cada propiedad nula significa "sin filtrar".
///
/// El objeto viaja entero hasta la clase de SQL porque la consulta de la LISTA y la del TOTAL
/// comparten el mismo predicado. Repartirlas es como se llega a que el contador marque un numero
/// que no cuadra con lo que el usuario ve, y las dos consultas esten bien por separado.
/// </summary>
public sealed record ActionAuditFilter(
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? ActionCode,
    string? SubjectKey,
    string? PerformedBy,
    bool OnlyPendingExternalEffect)
{
    /// <summary>
    /// Forma canonica de los filtros ACTIVOS. Es lo unico que cambia la forma del SQL, asi que
    /// es tambien lo que enumera la prueba de aislamiento para recorrer todas las variantes.
    /// </summary>
    public ActionAuditFilterShape Shape => new(
        FromUtc is not null,
        ToUtc is not null,
        !string.IsNullOrWhiteSpace(ActionCode),
        !string.IsNullOrWhiteSpace(SubjectKey),
        !string.IsNullOrWhiteSpace(PerformedBy),
        OnlyPendingExternalEffect);
}

/// <summary>Que filtros estan puestos. El VALOR nunca entra al texto del SQL: viaja parametrizado.</summary>
public sealed record ActionAuditFilterShape(
    bool HasFrom,
    bool HasTo,
    bool HasActionCode,
    bool HasSubjectKey,
    bool HasPerformedBy,
    bool OnlyPendingExternalEffect);

/// <summary>Filtros de la pantalla de accesos denegados.</summary>
public sealed record AccessDenialFilter(
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? PermissionCode,
    string? AttemptedBy,
    bool OnlyWouldHaveBeenBlocked)
{
    public AccessDenialFilterShape Shape => new(
        FromUtc is not null,
        ToUtc is not null,
        !string.IsNullOrWhiteSpace(PermissionCode),
        !string.IsNullOrWhiteSpace(AttemptedBy),
        OnlyWouldHaveBeenBlocked);
}

public sealed record AccessDenialFilterShape(
    bool HasFrom,
    bool HasTo,
    bool HasPermissionCode,
    bool HasAttemptedBy,
    bool OnlyWouldHaveBeenBlocked);

/// <summary>Un renglon de la bitacora de acciones, listo para la pantalla.</summary>
public sealed record ActionAuditItem(
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

/// <summary>Un renglon de la bitacora de accesos.</summary>
public sealed record AccessDenialItem(
    Guid PublicId,
    string PermissionCode,
    string Route,
    string HttpMethod,
    string AttemptedBy,
    string AttemptedByDisplay,
    bool EnforcementEnabled,
    bool Blocked,
    DateTime OccurredAtUtc);
