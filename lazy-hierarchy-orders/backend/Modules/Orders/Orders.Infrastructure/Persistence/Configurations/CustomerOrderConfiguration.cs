using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Entities;

namespace Orders.Infrastructure.Persistence.Configurations;

public sealed class CustomerOrderConfiguration : IEntityTypeConfiguration<CustomerOrder>
{
    public void Configure(EntityTypeBuilder<CustomerOrder> builder)
    {
        builder.ToTable("CustomerOrder");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.OrderNumber).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.CustomerName).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.TotalAmount).HasPrecision(18, 2);
        builder.Property(entity => entity.PlacedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.CreatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.DeletedAtUtc).HasColumnType("timestamptz");

        builder.HasIndex(entity => new { entity.TenantId, entity.OrderNumber }).IsUnique();

        // El indice del que vive el arbol: niveles de anio y mes (agregacion por rango de
        // fecha) y CTE del nivel de pedidos (ORDER BY placed_at_utc DESC, id DESC). Parcial
        // sobre los activos porque toda lectura filtra por is_active.
        builder.HasIndex(entity => new { entity.TenantId, entity.PlacedAtUtc, entity.Id })
            .HasFilter("is_active");

        builder.HasIndex(entity => new { entity.TenantId, entity.Status, entity.PlacedAtUtc })
            .HasFilter("is_active");
    }
}
