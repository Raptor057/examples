using LazyHierarchy.Kernel;

namespace Orders.Domain.Entities;

/// <summary>
/// Pedido. La tabla se llama customer_order y no order porque ORDER es palabra reservada de
/// SQL y nombrarla asi obligaria a entrecomillar el identificador en cada consulta.
/// </summary>
public sealed class CustomerOrder : BaseEntity, ITenantOwned, ISoftDeletable, IAuditable
{
    public long TenantId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime PlacedAtUtc { get; set; }

    public decimal TotalAmount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? DeletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<OrderLine> Lines { get; set; } = [];
}
