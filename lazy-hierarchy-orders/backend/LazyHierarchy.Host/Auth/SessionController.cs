using LazyHierarchy.Shared.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orders.Domain.Entities;

namespace LazyHierarchy.Host.Auth;

public sealed record DevTokenBody(string? TenantCode);

/// <summary>
/// Sesion del ejemplo: lista los tenants sembrados y emite un token para uno de ellos, para que
/// se pueda comprobar en vivo que el arbol de un tenant no muestra ni un renglon del otro.
/// Es andamiaje del ejemplo, no parte del patron.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class SessionController(AppDbContext db, DevTokenIssuer issuer) : ControllerBase
{
    [HttpGet("/api/session/tenants")]
    public async Task<IActionResult> GetTenants(CancellationToken cancellationToken)
    {
        // Tenant es entidad root: sin filtro de aislamiento, porque ES la tabla de tenants.
        var tenants = await db.Set<Tenant>()
            .AsNoTracking()
            .Where(tenant => tenant.IsActive)
            .OrderBy(tenant => tenant.Code)
            .Select(tenant => new { code = tenant.Code, name = tenant.Name })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            data = tenants,
            isSuccess = true,
            message = string.Empty,
            utcTimeStamp = DateTime.UtcNow
        });
    }

    [HttpPost("/api/session/token")]
    public async Task<IActionResult> IssueToken([FromBody] DevTokenBody body, CancellationToken cancellationToken)
    {
        var code = (body.TenantCode ?? string.Empty).Trim().ToLowerInvariant();

        var tenant = await db.Set<Tenant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Code == code && item.IsActive, cancellationToken);

        if (tenant is null)
        {
            return Ok(new
            {
                data = (object?)null,
                isSuccess = false,
                message = "No existe ese tenant.",
                utcTimeStamp = DateTime.UtcNow
            });
        }

        var (token, expiresAtUtc) = issuer.Issue(tenant.Id, tenant.Code);

        return Ok(new
        {
            data = new { accessToken = token, expiresAtUtc, tenantCode = tenant.Code, tenantName = tenant.Name },
            isSuccess = true,
            message = string.Empty,
            utcTimeStamp = DateTime.UtcNow
        });
    }
}
