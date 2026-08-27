using SaasAccessAudit.Kernel;

namespace Access.Domain.Entities;

/// <summary>
/// El catalogo de permisos, materializado en base para poder concederlo.
///
/// NO declara ITenantOwned, y no es un olvido: el catalogo define QUE SE PUEDE HACER en el
/// producto, no QUIEN puede hacerlo. Es igual para todos los tenants. Lo que si es por tenant
/// son los roles y las concesiones.
///
/// Las filas de esta tabla no se dan de alta a mano: las escribe el sembrador a partir del
/// catalogo en codigo (Access.Contracts.PermissionCatalog) en cada arranque. Un INSERT manual
/// aqui crea un permiso que ningun endpoint exige y que la pantalla de roles ignora.
/// </summary>
public sealed class Permission : BaseEntity, ISoftDeletable, IAuditable
{
    public string Code { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string Resource { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsDestructive { get; set; }

    /// <summary>
    /// Un permiso que desaparece del catalogo en codigo se DESACTIVA aqui, no se borra: las
    /// concesiones viejas y los renglones de bitacora que lo mencionan siguen siendo legibles.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime? DeletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
