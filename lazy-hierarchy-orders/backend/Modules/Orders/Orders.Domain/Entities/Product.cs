using LazyHierarchy.Kernel;

namespace Orders.Domain.Entities;

public sealed class Product : BaseEntity, ITenantOwned, ISoftDeletable, IAuditable
{
    public long TenantId { get; set; }

    public long CategoryId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? DeletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
