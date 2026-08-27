using LazyHierarchy.Kernel;

namespace Orders.Domain.Entities;

/// <summary>La hoja del arbol y la tabla grande del ejemplo: del orden de dos millones de filas.</summary>
public sealed class OrderLine : BaseEntity, ITenantOwned, ISoftDeletable, IAuditable
{
    public long TenantId { get; set; }

    public long OrderId { get; set; }

    public long ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? DeletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
