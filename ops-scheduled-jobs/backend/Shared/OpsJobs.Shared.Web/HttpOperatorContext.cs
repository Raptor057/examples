using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OpsJobs.Kernel;

namespace OpsJobs.Shared.Web;

/// <summary>
/// Quien pide, sale del TOKEN. Nunca de un query string ni del cuerpo: si el nombre del usuario
/// llegara por parametro, "ejecutar ahora" quedaria firmado por quien el cliente quisiera y la
/// bitacora dejaria de servir para lo unico que sirve (rules/server-side-data-boundaries).
///
/// Sin peticion autenticada devuelve el operador anonimo, que no puede nada: fail-closed.
/// </summary>
public sealed class HttpOperatorContext(IHttpContextAccessor accessor) : IOperatorContext
{
    public OperatorIdentity Current
    {
        get
        {
            var user = accessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true) return OperatorIdentity.Anonymous;

            var userName = user.FindFirstValue(ClaimTypes.Name)
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? string.Empty;

            return new OperatorIdentity(userName, user.IsInRole(OpsRoles.Administrator));
        }
    }
}
