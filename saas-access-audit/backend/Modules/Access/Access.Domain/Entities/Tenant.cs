using SaasAccessAudit.Kernel;

namespace Access.Domain.Entities;

/// <summary>
/// Entidad ROOT: no declara ITenantOwned porque ES la tabla de tenants. Aqui no falta el filtro
/// de aislamiento; esta ausente a proposito.
/// </summary>
public sealed class Tenant : BaseEntity, ISoftDeletable, IAuditable
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? DeletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
