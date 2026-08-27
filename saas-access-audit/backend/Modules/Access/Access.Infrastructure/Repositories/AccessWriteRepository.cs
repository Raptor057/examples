using Access.Domain.Entities;
using Access.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using SaasAccessAudit.Shared.Persistence;

namespace Access.Infrastructure.Repositories;

/// <summary>
/// Escrituras del modulo. Igual que las lecturas: ni un Where por tenant, ni una asignacion de
/// tenant. El global query filter acota lo que se encuentra y el interceptor sella lo que se
/// inserta, los dos desde el contexto de la peticion.
/// </summary>
internal sealed class AccessWriteRepository(AppDbContext db) : IAccessWriteRepository
{
    public Task<AppUser?> FindUserAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        db.Set<AppUser>().FirstOrDefaultAsync(user => user.PublicId == publicId, cancellationToken);

    public Task<Role?> FindRoleAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        db.Set<Role>().FirstOrDefaultAsync(role => role.PublicId == publicId, cancellationToken);

    public Task<Permission?> FindPermissionAsync(string code, CancellationToken cancellationToken = default) =>
        db.Set<Permission>().FirstOrDefaultAsync(permission => permission.Code == code, cancellationToken);

    public async Task<bool> DeactivateUserAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        // FILTRO POR EL ESTADO PREVIO. Sin el, el segundo clic vuelve a escribir la fila, vuelve
        // a llamar al proveedor de identidad y vuelve a dejar un renglon de bitacora, y quien lo
        // lea despues no puede saber que el segundo no hizo nada.
        if (!user.IsActive) return false;

        user.IsActive = false;
        user.DeactivatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> SetRolePermissionAsync(
        long roleId, long permissionId, bool granted, CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters SOLO para saltarse el filtro de activos: una concesion revocada esta
        // apagada, y hay que encontrarla para poder reactivarla en vez de insertar una segunda
        // fila que chocaria con la llave unica. El aislamiento por tenant se repone a mano en el
        // mismo Where, porque IgnoreQueryFilters los quita TODOS y olvidarlo aqui seria una fuga.
        var tenantId = db.CurrentTenantIdForExplicitFilters;

        var existing = await db.Set<RolePermission>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                grant => grant.TenantId == tenantId
                    && grant.RoleId == roleId
                    && grant.PermissionId == permissionId,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            // Revocar algo que nunca se concedio no es un cambio, y no tiene por que auditarse.
            if (!granted) return false;

            db.Set<RolePermission>().Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                IsActive = true
            });

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        if (existing.IsActive == granted) return false;

        existing.IsActive = granted;
        existing.DeletedAtUtc = granted ? null : DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
