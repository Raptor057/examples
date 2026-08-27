using Audit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audit.Infrastructure.Persistence.Configurations;

/// <summary>
/// La tabla de decisiones de acceso. Sin columna de motivo, y esa ausencia es la razon de que sea
/// una tabla aparte: un intento bloqueado no tiene motivo porque la persona nunca llego a
/// escribirlo.
/// </summary>
public sealed class AccessDenialEntryConfiguration : IEntityTypeConfiguration<AccessDenialEntry>
{
    public void Configure(EntityTypeBuilder<AccessDenialEntry> builder)
    {
        builder.ToTable("AccessDenialEntry");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.PermissionCode).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.Route).HasMaxLength(300).IsRequired();
        builder.Property(entity => entity.HttpMethod).HasMaxLength(10).IsRequired();
        builder.Property(entity => entity.AttemptedBy).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.AttemptedByDisplay).HasMaxLength(150).IsRequired();

        builder.Property(entity => entity.OccurredAtUtc)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        builder.HasIndex(entity => new { entity.TenantId, entity.OccurredAtUtc, entity.Id })
            .HasDatabaseName("ix_access_denial_entry_tenant_recent");

        builder.HasIndex(entity => new { entity.TenantId, entity.PermissionCode, entity.OccurredAtUtc })
            .HasDatabaseName("ix_access_denial_entry_tenant_permission");

        // "Que se habria bloqueado si el flag estuviera encendido" - la lista que se revisa antes
        // de encenderlo. Filtrado, porque es una minoria de las filas y se consulta sola.
        builder.HasIndex(entity => new { entity.TenantId, entity.OccurredAtUtc })
            .HasDatabaseName("ix_access_denial_entry_would_have_blocked")
            .HasFilter("blocked = false");
    }
}
