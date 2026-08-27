using Access.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Access.Infrastructure.Persistence.Configurations;

/// <summary>
/// AppUser lleva is_active como ESTADO DE NEGOCIO, no como borrado logico: la entidad no declara
/// ISoftDeletable y por eso el DbContext no le aplica el filtro de activos. Un usuario
/// desactivado tiene que seguir apareciendo en la pantalla, en solo lectura, o nadie podria
/// comprobar que se desactivo.
/// </summary>
public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("AppUser");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Username).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.DisplayName).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.Email).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.CreatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.UpdatedAtUtc).HasColumnType("timestamptz");
        builder.Property(entity => entity.DeactivatedAtUtc).HasColumnType("timestamptz");

        builder.HasIndex(entity => new { entity.TenantId, entity.Username }).IsUnique();

        // La pantalla ordena por nombre de usuario dentro del tenant; el indice le sirve tal cual
        // al ORDER BY y al OFFSET/FETCH que lo sigue.
        builder.HasIndex(entity => new { entity.TenantId, entity.IsActive, entity.Username });
    }
}
