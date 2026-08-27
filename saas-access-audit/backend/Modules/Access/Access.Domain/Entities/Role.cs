using SaasAccessAudit.Kernel;

namespace Access.Domain.Entities;

/// <summary>
/// Rol, POR TENANT. Decision escrita del ejemplo: cada empresa define sus propios roles, asi que
/// la entidad declara su ambito y el DbContext le aplica el filtro. La alternativa -roles
/// globales del producto- es igual de valida y se resuelve quitando ITenantOwned; lo que no vale
/// es dejarlo sin decidir, porque el dia que dos tenants quieran el mismo nombre de rol con
/// permisos distintos ya no hay marcha atras barata.
/// </summary>
public sealed class Role : BaseEntity, ITenantOwned, ISoftDeletable, IAuditable
{
    public long TenantId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? DeletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
