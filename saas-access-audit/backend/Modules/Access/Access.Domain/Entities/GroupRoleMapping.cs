using SaasAccessAudit.Kernel;

namespace Access.Domain.Entities;

/// <summary>
/// El primer salto de la cadena: que PERTENENCIA otorga que rol.
///
///     pertenencia (del token) -&gt; rol -&gt; permiso -&gt; politica del endpoint
///
/// El nombre del grupo es el que viaja en el token, y lo emite el proveedor de identidad. Aqui
/// solo se traduce a un rol de este producto. Por eso un permiso nunca se asigna directo a una
/// persona: las personas llegan por sus pertenencias.
/// </summary>
public sealed class GroupRoleMapping : BaseEntity, ITenantOwned, ISoftDeletable, IAuditable
{
    public long TenantId { get; set; }

    /// <summary>Nombre del grupo tal como llega en el claim del token.</summary>
    public string GroupName { get; set; } = string.Empty;

    public long RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? DeletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
