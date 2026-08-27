using Audit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audit.Infrastructure.Persistence.Configurations;

/// <summary>
/// La tabla de la bitacora de acciones. Tres cosas de aqui no son cosmeticas.
///
/// 1. EL MOTIVO LLEVA CHECK EN LA BASE, no solo validacion en el handler. El handler cubre el
///    camino que existe hoy; el CHECK cubre el que alguien escriba manana sin pasar por el.
/// 2. LA FECHA LA PONE EL MOTOR (now()). Con varias replicas, cada una con su reloj, sellar la
///    hora en el proceso produce una linea de tiempo que no cuadra consigo misma.
/// 3. EL INDICE DE PENDIENTES ES FILTRADO. La pregunta "que quedo descuadrado" tiene que
///    responderse en una consulta barata sobre un punado de filas, no barriendo la bitacora
///    entera; y como el indice solo contiene los pendientes, no crece con el historico.
/// </summary>
public sealed class ActionAuditEntryConfiguration : IEntityTypeConfiguration<ActionAuditEntry>
{
    public void Configure(EntityTypeBuilder<ActionAuditEntry> builder)
    {
        builder.ToTable("ActionAuditEntry", table => table.HasCheckConstraint(
            "ck_action_audit_entry_reason",
            "length(btrim(reason)) > 0"));

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.ActionCode).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.SubjectType).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.SubjectKey).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.SubjectLabel).HasMaxLength(300).IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.PerformedBy).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.PerformedByDisplay).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.AuthorizedBy).HasMaxLength(120);
        builder.Property(entity => entity.ExternalEffectName).HasMaxLength(120);
        builder.Property(entity => entity.ExternalEffectError).HasMaxLength(1000);

        builder.Property(entity => entity.OccurredAtUtc)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // "Que paso ultimamente" - la pantalla, siempre lo mas reciente primero. El id va al
        // final para que el ORDER BY sea UNICO: sin desempate, dos filas del mismo instante
        // pueden repetirse en una pagina y desaparecer de la siguiente.
        builder.HasIndex(entity => new { entity.TenantId, entity.OccurredAtUtc, entity.Id })
            .HasDatabaseName("ix_action_audit_entry_tenant_recent");

        // "Que le paso a esta entidad" - el filtro por la llave de lo afectado.
        builder.HasIndex(entity => new { entity.TenantId, entity.SubjectKey, entity.OccurredAtUtc })
            .HasDatabaseName("ix_action_audit_entry_tenant_subject");

        // "Quien hizo esto" y el cruce por codigo de accion con la bitacora de accesos.
        builder.HasIndex(entity => new { entity.TenantId, entity.ActionCode, entity.OccurredAtUtc })
            .HasDatabaseName("ix_action_audit_entry_tenant_action");

        // "Que quedo pendiente" - indice FILTRADO sobre los efectos externos sin aplicar.
        builder.HasIndex(entity => new { entity.TenantId, entity.OccurredAtUtc })
            .HasDatabaseName("ix_action_audit_entry_pending_external_effect")
            .HasFilter("external_effect_applied = false");
    }
}
