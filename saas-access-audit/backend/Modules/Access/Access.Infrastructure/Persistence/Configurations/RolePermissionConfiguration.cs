using Access.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Access.Infrastructure.Persistence.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermission");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CreatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.DeletedAtUtc).HasColumnType("timestamptz");

        // La unicidad NO filtra por activos a proposito: revocar apaga la fila y volver a
        // conceder tiene que reactivar la MISMA, no crear una segunda.
        builder.HasIndex(entity => new { entity.TenantId, entity.RoleId, entity.PermissionId }).IsUnique();

        // Indice de la consulta caliente: los permisos efectivos se calculan en CADA peticion,
        // asi que su plan importa mas que el de cualquier pantalla.
        builder.HasIndex(entity => new { entity.TenantId, entity.RoleId })
            .HasFilter("is_active");
    }
}
