using System.Security.Claims;
using SaasAccessAudit.Kernel;
using Microsoft.AspNetCore.Http;

namespace SaasAccessAudit.Shared.Web;

/// <summary>
/// Publica en el contexto de la peticion las DOS cosas que salen del token y de ningun otro lado:
/// el tenant (aisla los datos) y la identidad con sus pertenencias (decide los permisos y firma
/// la bitacora).
///
/// Corre DESPUES de UseAuthentication: antes, HttpContext.User todavia no tiene claims, el tenant
/// quedaria vacio -y todo filtraria contra el contexto sistema- y la identidad tambien, con lo que
/// cada renglon de bitacora diria "anonimo".
///
/// El token trae PERTENENCIAS, no permisos. Ver <see cref="UserContext"/>.
/// </summary>
public sealed class RequestContextMiddleware(RequestDelegate next)
{
    public const string TenantClaim = "tenant_id";
    public const string GroupClaim = "groups";
    public const string DisplayNameClaim = "display_name";

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContextAccessor tenantAccessor,
        IUserContextAccessor userAccessor)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var rawTenant = context.User.FindFirstValue(TenantClaim);
            if (long.TryParse(rawTenant, out var tenantId))
                tenantAccessor.Current = new TenantContext(tenantId);

            var username = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");

            if (!string.IsNullOrWhiteSpace(username))
            {
                // El claim de pertenencia es REPETIBLE: una persona pertenece a varios grupos y
                // sus permisos son la union de los que le dan todos ellos.
                var groups = context.User.FindAll(GroupClaim)
                    .Select(claim => claim.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToArray();

                userAccessor.Current = new UserContext(
                    username,
                    context.User.FindFirstValue(DisplayNameClaim) ?? username,
                    groups);
            }
        }

        await next(context).ConfigureAwait(false);
    }
}
