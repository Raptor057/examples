using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Entities;

namespace Orders.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Product");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Sku).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.UnitPrice).HasPrecision(18, 2);
        builder.Property(entity => entity.CreatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.DeletedAtUtc).HasColumnType("timestamptz");

        builder.HasIndex(entity => new { entity.TenantId, entity.Sku }).IsUnique();

        // Sirve al nivel "producto": productos de una categoria, ya ordenados por nombre.
        builder.HasIndex(entity => new { entity.TenantId, entity.CategoryId, entity.Name });

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(entity => entity.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
