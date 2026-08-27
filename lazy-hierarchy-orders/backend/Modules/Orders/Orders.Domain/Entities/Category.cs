using LazyHierarchy.Kernel;

namespace Orders.Domain.Entities;

public sealed class Category : BaseEntity, ITenantOwned, ISoftDeletable, IAuditable
{
    public long TenantId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? DeletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
