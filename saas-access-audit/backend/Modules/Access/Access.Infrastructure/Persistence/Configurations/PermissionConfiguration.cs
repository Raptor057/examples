using Access.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Access.Infrastructure.Persistence.Configurations;

/// <summary>
/// Catalogo GLOBAL del producto: sin tenant_id y sin filtro de aislamiento, igual que Tenant.
/// Define que se puede hacer, no quien puede hacerlo, y eso es identico para todas las empresas.
///
/// La unicidad del codigo es global por el mismo motivo, y es la que hace idempotente al
/// sembrador: al arrancar compara el catalogo de codigo contra esta tabla y solo inserta lo que
/// falta.
/// </summary>
public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permission");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Code).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.Module).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.Resource).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.Action).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.DisplayName).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.DeletedAtUtc).HasColumnType("timestamptz");

        builder.HasIndex(entity => entity.Code).IsUnique();
    }
}
