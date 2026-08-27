using LazyHierarchy.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LazyHierarchy.Shared.Persistence;

/// <summary>
/// Mantiene las fechas de auditoria en UTC, sella el tenant al insertar y convierte el borrado
/// fisico en logico. Asi ITenantOwned, IAuditable e ISoftDeletable funcionan sin que ningun
/// handler se acuerde de ellos.
/// </summary>
public sealed class AuditSoftDeleteInterceptor(ITenantContextAccessor tenantAccessor) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData, tenantAccessor);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData, tenantAccessor);
        return base.SavingChanges(eventData, result);
    }

    private static void Apply(DbContextEventData eventData, ITenantContextAccessor tenantAccessor)
    {
        var context = eventData.Context;
        if (context is null) return;

        var nowUtc = DateTime.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries())
        {
            // El tenant de una fila nueva sale del contexto autenticado, NUNCA del cuerpo de la
            // peticion. Si el seeder o una migracion ya lo asignaron a mano, se respeta.
            if (entry.State == EntityState.Added
                && entry.Entity is ITenantOwned tenantOwned
                && tenantOwned.TenantId == SystemContext.TenantId
                && tenantAccessor.Current is { } tenantContext)
            {
                tenantOwned.TenantId = tenantContext.TenantId;
            }

            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added) auditable.CreatedAtUtc = nowUtc;
                if (entry.State is EntityState.Added or EntityState.Modified) auditable.UpdatedAtUtc = nowUtc;
            }

            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable softDeletable)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsActive = false;
                softDeletable.DeletedAtUtc = nowUtc;
            }
        }
    }
}
