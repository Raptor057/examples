using Access.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Access.Infrastructure.Persistence.Configurations;

public sealed class GroupRoleMappingConfiguration : IEntityTypeConfiguration<GroupRoleMapping>
{
    public void Configure(EntityTypeBuilder<GroupRoleMapping> builder)
    {
        builder.ToTable("GroupRoleMapping");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.GroupName).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.DeletedAtUtc).HasColumnType("timestamptz");

        builder.HasIndex(entity => new { entity.TenantId, entity.GroupName, entity.RoleId }).IsUnique();

        // Primer salto del calculo de permisos: del grupo del token al rol.
        builder.HasIndex(entity => new { entity.TenantId, entity.GroupName })
            .HasFilter("is_active");
    }
}
