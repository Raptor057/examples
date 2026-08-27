using System.Security.Claims;
using LazyHierarchy.Kernel;
using Microsoft.AspNetCore.Http;

namespace LazyHierarchy.Shared.Web;

/// <summary>
/// Publica el tenant del token en el contexto de la peticion. Corre DESPUES de
/// UseAuthentication: antes, HttpContext.User todavia no tiene claims y todo filtraria contra
/// el contexto sistema. El tenant sale del claim y de ningun otro lado: no hay parametro de
/// query ni campo de cuerpo que pueda cambiarlo.
/// </summary>
public sealed class TenantClaimsMiddleware(RequestDelegate next)
{
    public const string TenantClaim = "tenant_id";

    public async Task InvokeAsync(HttpContext context, ITenantContextAccessor tenantAccessor)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var raw = context.User.FindFirstValue(TenantClaim);
            if (long.TryParse(raw, out var tenantId))
                tenantAccessor.Current = new TenantContext(tenantId);
        }

        await next(context).ConfigureAwait(false);
    }
}
