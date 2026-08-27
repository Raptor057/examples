using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Entities;

namespace Orders.Infrastructure.Persistence.Configurations;

public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLine");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.UnitPrice).HasPrecision(18, 2);
        builder.Property(entity => entity.LineTotal).HasPrecision(18, 2);
        builder.Property(entity => entity.CreatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.DeletedAtUtc).HasColumnType("timestamptz");

        // Sirve a la hoja (lineas de un pedido) y a los dos LEFT JOIN LATERAL que enriquecen la
        // pagina del nivel de pedidos.
        builder.HasIndex(entity => new { entity.TenantId, entity.OrderId })
            .HasFilter("is_active");

        // Sirve al EXISTS del CTE (pedidos que contienen un producto) y a la agregacion de los
        // niveles de categoria y producto.
        builder.HasIndex(entity => new { entity.TenantId, entity.ProductId, entity.OrderId })
            .HasFilter("is_active");

        builder.HasOne<CustomerOrder>()
            .WithMany(order => order.Lines)
            .HasForeignKey(entity => entity.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(entity => entity.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
