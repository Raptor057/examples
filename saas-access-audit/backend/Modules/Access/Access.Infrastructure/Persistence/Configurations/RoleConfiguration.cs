using Access.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Access.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Role");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Code).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.DeletedAtUtc).HasColumnType("timestamptz");

        // Unicidad POR TENANT: dos empresas pueden llamar "admin" a roles con permisos distintos.
        builder.HasIndex(entity => new { entity.TenantId, entity.Code }).IsUnique();
    }
}
