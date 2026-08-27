using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Entities;

namespace Orders.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Category");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Code).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.DeletedAtUtc).HasColumnType("timestamptz");

        // El indice compuesto empieza SIEMPRE por la clave del ambito: toda consulta filtra
        // por tenant antes que por cualquier otra cosa.
        builder.HasIndex(entity => new { entity.TenantId, entity.Code }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.Name });
    }
}
