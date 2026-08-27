using LazyHierarchy.Kernel;

namespace Orders.Domain.Entities;

/// <summary>
/// Entidad root: NO pertenece a ningun tenant, es la tabla de tenants. No declara ITenantOwned
/// a proposito, y por eso el chasis no le pone filtro de aislamiento. Dejarlo escrito evita que
/// alguien lo lea como un filtro olvidado.
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
