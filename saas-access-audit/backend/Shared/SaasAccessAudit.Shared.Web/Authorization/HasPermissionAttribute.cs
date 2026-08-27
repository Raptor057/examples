using Microsoft.AspNetCore.Http;
using SaasAccessAudit.Kernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace SaasAccessAudit.Shared.Web.Authorization;

/// <summary>
/// La politica del endpoint. Se declara con el codigo EXACTO del catalogo de permisos:
///
///     [HasPermission(PermissionCatalog.UsersDeactivate)]
///
/// Se usa la constante y no el literal a proposito. Un typo en el codigo se ve exactamente igual
/// que "no tienes permiso" -es el punto 6 del diagnostico de un 403- y con la constante el
/// compilador lo atrapa antes de que nadie lo reporte. Ademas hay una prueba que recorre los
/// controllers y falla si algun endpoint exige un permiso que el catalogo no declara.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class HasPermissionAttribute(string permissionCode) : Attribute, IFilterFactory
{
    public string PermissionCode { get; } = permissionCode;

    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider) =>
        new PermissionAuthorizationFilter(
            PermissionCode,
            serviceProvider.GetRequiredService<IPermissionEvaluator>());
}

/// <summary>
/// UNICO punto donde se decide si la peticion pasa. El filtro no sabe de roles ni de grupos:
/// le pregunta al evaluador, que ademas deja el rastro de lo negado.
/// </summary>
internal sealed class PermissionAuthorizationFilter(
    string permissionCode,
    IPermissionEvaluator evaluator) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;

        var decision = await evaluator.EvaluateAsync(
            permissionCode,
            request.Path.Value ?? string.Empty,
            request.Method,
            context.HttpContext.RequestAborted).ConfigureAwait(false);

        if (decision.Allowed) return;

        // 403 con el MISMO envelope que cualquier otra respuesta: el cliente no necesita un
        // camino especial para leer un rechazo. El codigo del permiso va en el mensaje porque
        // es el dato que convierte un "no tienes permiso" en un diagnostico.
        context.Result = new ObjectResult(new
        {
            data = (object?)null,
            isSuccess = false,
            message = $"No tienes el permiso {permissionCode}.",
            utcTimeStamp = DateTime.UtcNow
        })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}
