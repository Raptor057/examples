using Access.Domain.Entities;
using Access.Domain.Models;
using Access.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using SaasAccessAudit.Kernel;
using SaasAccessAudit.Shared.Persistence;

namespace Access.Infrastructure.Repositories;

/// <summary>
/// Lecturas del modulo, por EF Core.
///
/// NI UN SOLO Where por tenant en todo el archivo. El global query filter lo pone en cada
/// consulta desde el contexto de la peticion. Ese contraste con el repositorio de bitacora -que
/// baja a SQL crudo y por eso tiene que componer el filtro a mano con TenantScope- es la leccion
/// que este ejemplo comparte con su hermano: donde hay aislamiento que olvidar, lo aplica el ORM.
/// </summary>
internal sealed class AccessReadRepository(AppDbContext db, ITenantContextAccessor tenantAccessor)
    : IAccessReadRepository
{
    public async Task<IReadOnlyList<string>> GetEffectivePermissionCodesAsync(
        IReadOnlyList<string> groupNames, CancellationToken cancellationToken = default)
    {
        if (groupNames.Count == 0) return [];

        // Los nombres de grupo se comparan siempre en minusculas. PostgreSQL distingue
        // mayusculas y un proveedor de identidad que un dia mande "SaaS-Admins" en vez de
        // "saas-admins" dejaria a alguien sin permisos sin que nadie cambiara nada.
        var normalized = groupNames
            .Select(group => group.Trim().ToLowerInvariant())
            .Where(group => group.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalized.Length == 0) return [];

        // La cadena entera, en una consulta: pertenencia -> rol -> permiso.
        var query =
            from mapping in db.Set<GroupRoleMapping>()
            where normalized.Contains(mapping.GroupName)
            join role in db.Set<Role>() on mapping.RoleId equals role.Id
            join grant in db.Set<RolePermission>() on role.Id equals grant.RoleId
            join permission in db.Set<Permission>() on grant.PermissionId equals permission.Id
            select permission.Code;

        return await query
            .AsNoTracking()
            .Distinct()
            .OrderBy(code => code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<string> GetTenantCodeAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantAccessor.Current?.TenantId ?? SystemContext.TenantId;

        // Tenant es entidad root: no lleva filtro de aislamiento, asi que aqui SI se compara el
        // identificador de forma explicita. Sale del contexto de la peticion, no de un parametro.
        var code = await db.Set<Tenant>()
            .AsNoTracking()
            .Where(tenant => tenant.Id == tenantId)
            .Select(tenant => tenant.Code)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return code ?? string.Empty;
    }

    public async Task<IReadOnlyList<RoleWithGrants>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await db.Set<Role>()
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new
            {
                role.PublicId,
                role.Code,
                role.Name,
                role.Id
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var roleIds = roles.Select(role => role.Id).ToArray();

        var grants = await (
            from grant in db.Set<RolePermission>()
            where roleIds.Contains(grant.RoleId)
            join permission in db.Set<Permission>() on grant.PermissionId equals permission.Id
            select new { grant.RoleId, permission.Code })
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var groups = await db.Set<GroupRoleMapping>()
            .AsNoTracking()
            .Where(mapping => roleIds.Contains(mapping.RoleId))
            .Select(mapping => new { mapping.RoleId, mapping.GroupName })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return roles
            .Select(role => new RoleWithGrants(
                role.PublicId,
                role.Code,
                role.Name,
                groups.Where(item => item.RoleId == role.Id).Select(item => item.GroupName).Order().ToList(),
                grants.Where(item => item.RoleId == role.Id).Select(item => item.Code).Order().ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<PermissionRow>> GetPermissionsAsync(CancellationToken cancellationToken = default) =>
        await db.Set<Permission>()
            .AsNoTracking()
            .OrderBy(permission => permission.Module)
            .ThenBy(permission => permission.Resource)
            .ThenBy(permission => permission.Action)
            .Select(permission => new PermissionRow(
                permission.Code,
                permission.Module,
                permission.Resource,
                permission.Action,
                permission.DisplayName,
                permission.IsDestructive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<PagedResult<UserRow>> GetUsersPageAsync(
        string? search, bool onlyActive, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = db.Set<AppUser>().AsNoTracking();

        // El predicado se arma UNA vez y lo comparten el conteo y la pagina. Copiarlo en las dos
        // consultas es como se llega a que el total marque un numero que no cuadra con la lista,
        // y las dos esten bien por separado.
        if (onlyActive)
            query = query.Where(user => user.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(user =>
                EF.Functions.ILike(user.Username, pattern)
                || EF.Functions.ILike(user.DisplayName, pattern)
                || EF.Functions.ILike(user.Email, pattern));
        }

        var totalCount = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);

        // ORDER BY unico: el nombre de usuario ya es unico por tenant, asi que la paginacion no
        // puede repetir ni omitir filas entre paginas.
        var pageUsers = await query
            .OrderBy(user => user.Username)
            .Skip(Paging.Offset(pageNumber, pageSize))
            .Take(pageSize)
            .Select(user => new
            {
                user.Id,
                user.PublicId,
                user.Username,
                user.DisplayName,
                user.Email,
                user.IsActive,
                user.DeactivatedAtUtc
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var userIds = pageUsers.Select(user => user.Id).ToArray();

        // Las pertenencias y los roles se traen SOLO para la pagina visible.
        var memberships = await (
            from membership in db.Set<AppUserGroup>()
            where userIds.Contains(membership.UserId)
            select new { membership.UserId, membership.GroupName })
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var groupRoles = await (
            from mapping in db.Set<GroupRoleMapping>()
            join role in db.Set<Role>() on mapping.RoleId equals role.Id
            select new { mapping.GroupName, role.Name })
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = pageUsers
            .Select(user =>
            {
                var groups = memberships
                    .Where(membership => membership.UserId == user.Id)
                    .Select(membership => membership.GroupName)
                    .Order()
                    .ToList();

                var roles = groupRoles
                    .Where(item => groups.Contains(item.GroupName, StringComparer.Ordinal))
                    .Select(item => item.Name)
                    .Distinct(StringComparer.Ordinal)
                    .Order()
                    .ToList();

                return new UserRow(
                    user.PublicId,
                    user.Username,
                    user.DisplayName,
                    user.Email,
                    user.IsActive,
                    user.DeactivatedAtUtc,
                    groups,
                    roles);
            })
            .ToList();

        return new PagedResult<UserRow>(items, totalCount, pageNumber, pageSize);
    }
}
