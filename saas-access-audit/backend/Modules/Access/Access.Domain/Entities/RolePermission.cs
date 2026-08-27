using SaasAccessAudit.Kernel;

namespace Access.Domain.Entities;

/// <summary>
/// La concesion: que rol tiene que permiso.
///
/// Revocar apaga la fila (soft delete) en vez de borrarla, por dos razones: la convencion de base
/// del catalogo prohibe el DELETE fisico, y volver a conceder despues reactiva la MISMA fila, con
/// lo que la llave unica por tenant + rol + permiso sigue valiendo.
/// </summary>
public sealed class RolePermission : BaseEntity, ITenantOwned, ISoftDeletable, IAuditable
{
    public long TenantId { get; set; }

    public long RoleId { get; set; }

    public long PermissionId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? DeletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
