using Access.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaasAccessAudit.Kernel;
using SaasAccessAudit.Shared.Persistence;

namespace SaasAccessAudit.Host.Auth;

public sealed record DevTokenBody(string? TenantCode, string? Username);

/// <summary>
/// Sesion del ejemplo: lista los tenants y sus usuarios, y emite un token para uno de ellos.
///
/// Es ANDAMIAJE, no parte del patron. Existe para poder cambiar de persona en la pantalla y
/// comprobar en vivo las tres situaciones que el ejemplo quiere ensenar: quien pudo, quien no
/// pudo, y que quedo registrado en cada caso.
///
/// La lectura de usuarios de aqui rompe a proposito una regla del propio ejemplo: consulta la
/// base saltandose el contexto de tenant, porque todavia no hay token del que sacarlo. Es el
/// unico lugar donde eso pasa, y por eso vive en el Host y no en un modulo.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class SessionController(
    AppDbContext db,
    ITenantContextAccessor tenantAccessor,
    DevTokenIssuer issuer) : ControllerBase
{
    [HttpGet("/api/session/tenants")]
    public async Task<IActionResult> GetTenants(CancellationToken cancellationToken)
    {
        // Tenant es entidad root: sin filtro de aislamiento, porque ES la tabla de tenants.
        var tenants = await db.Set<Tenant>()
            .AsNoTracking()
            .OrderBy(tenant => tenant.Code)
            .Select(tenant => new { code = tenant.Code, name = tenant.Name, id = tenant.Id })
            .ToListAsync(cancellationToken);

        var result = new List<object>(tenants.Count);
        foreach (var tenant in tenants)
        {
            // Se establece el contexto para leer COMO ese tenant, en vez de saltarse los filtros
            // con IgnoreQueryFilters: asi la lectura recorre el mismo camino que la aplicacion.
            tenantAccessor.Current = new TenantContext(tenant.id);
            try
            {
                var users = await (
                    from user in db.Set<AppUser>()
                    orderby user.Username
                    select new
                    {
                        username = user.Username,
                        displayName = user.DisplayName,
                        isActive = user.IsActive,
                        groups = db.Set<AppUserGroup>()
                            .Where(membership => membership.UserId == user.Id)
                            .Select(membership => membership.GroupName)
                            .ToList()
                    })
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                result.Add(new { code = tenant.code, name = tenant.name, users });
            }
            finally
            {
                tenantAccessor.Current = null;
            }
        }

        return Ok(new { data = result, isSuccess = true, message = string.Empty, utcTimeStamp = DateTime.UtcNow });
    }

    [HttpPost("/api/session/token")]
    public async Task<IActionResult> IssueToken([FromBody] DevTokenBody body, CancellationToken cancellationToken)
    {
        var code = (body.TenantCode ?? string.Empty).Trim().ToLowerInvariant();
        var username = (body.Username ?? string.Empty).Trim().ToLowerInvariant();

        var tenant = await db.Set<Tenant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Code == code, cancellationToken);

        if (tenant is null)
            return Ok(new { data = (object?)null, isSuccess = false, message = "No existe ese tenant.", utcTimeStamp = DateTime.UtcNow });

        tenantAccessor.Current = new TenantContext(tenant.Id);
        try
        {
            var user = await db.Set<AppUser>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Username == username, cancellationToken);

            if (user is null)
                return Ok(new { data = (object?)null, isSuccess = false, message = "No existe ese usuario en esa empresa.", utcTimeStamp = DateTime.UtcNow });

            var groups = await db.Set<AppUserGroup>()
                .AsNoTracking()
                .Where(membership => membership.UserId == user.Id)
                .Select(membership => membership.GroupName)
                .ToListAsync(cancellationToken);

            var (token, expiresAtUtc) = issuer.Issue(tenant.Id, tenant.Code, user.Username, user.DisplayName, groups);

            return Ok(new
            {
                data = new
                {
                    accessToken = token,
                    expiresAtUtc,
                    tenantCode = tenant.Code,
                    tenantName = tenant.Name,
                    username = user.Username,
                    displayName = user.DisplayName,
                    groups
                },
                isSuccess = true,
                message = string.Empty,
                utcTimeStamp = DateTime.UtcNow
            });
        }
        finally
        {
            tenantAccessor.Current = null;
        }
    }
}
