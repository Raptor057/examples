using Access.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Access.Infrastructure.Persistence.Configurations;

public sealed class AppUserGroupConfiguration : IEntityTypeConfiguration<AppUserGroup>
{
    public void Configure(EntityTypeBuilder<AppUserGroup> builder)
    {
        builder.ToTable("AppUserGroup");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.GroupName).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.DeletedAtUtc).HasColumnType("timestamptz");

        builder.HasIndex(entity => new { entity.TenantId, entity.UserId, entity.GroupName }).IsUnique();
    }
}
