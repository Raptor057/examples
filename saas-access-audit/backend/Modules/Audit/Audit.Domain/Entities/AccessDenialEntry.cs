using SaasAccessAudit.Kernel;

namespace Audit.Domain.Entities;

/// <summary>
/// LA BITACORA DE DECISIONES DE ACCESO: lo que se bloqueo, y lo que se HABRIA bloqueado.
///
/// Tabla aparte de la de acciones a proposito. El razonamiento completo esta en
/// Audit.Contracts.IAccessDecisionLog; en corto: la de acciones exige un motivo que un intento
/// bloqueado no tiene, y su pantalla ofrece efectos externos que un rechazo no produce.
///
/// Tambien es append-only, con el mismo disparador en la base.
/// </summary>
public sealed class AccessDenialEntry : BaseEntity, ITenantOwned
{
    public long TenantId { get; set; }

    public string PermissionCode { get; set; } = string.Empty;

    public string Route { get; set; } = string.Empty;

    public string HttpMethod { get; set; } = string.Empty;

    public string AttemptedBy { get; set; } = string.Empty;

    public string AttemptedByDisplay { get; set; } = string.Empty;

    /// <summary>Estado del flag de enforcement en el momento de decidir.</summary>
    public bool EnforcementEnabled { get; set; }

    /// <summary>
    /// Si la peticion se corto de verdad. En modo auditoria vale false y el renglon significa
    /// "esto se habria bloqueado": es la lista que se revisa antes de encender el flag.
    /// </summary>
    public bool Blocked { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}
