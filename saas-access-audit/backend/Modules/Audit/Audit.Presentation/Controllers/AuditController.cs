using Access.Contracts;
using Audit.Application.UseCases.GetAccessDenials;
using Audit.Application.UseCases.GetActionAudit;
using Common.Messaging;
using Common.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasAccessAudit.Shared.Web;
using SaasAccessAudit.Shared.Web.Authorization;

namespace Audit.Presentation.Controllers;

/// <summary>
/// La pantalla de consulta de la bitacora, en dos endpoints: lo que se hizo y lo que no se pudo
/// hacer.
///
/// LA BITACORA LLEVA PERMISO PROPIO DE CONSULTA. Registra quien hizo que, y no todos deben verlo:
/// en la mayoria de las organizaciones esa lista la lee auditoria y la lee el administrador, no
/// cualquiera que entre a la consola.
///
/// El codigo del permiso viene de Access.Contracts, que es la unica via legitima entre modulos.
/// El catalogo es del PRODUCTO, no del modulo que administra roles, y por eso el modulo Audit
/// declara ahi su permiso en vez de mantener una lista propia que un dia se desincronizaria.
///
/// Solo hay GET. No existe endpoint para editar ni para borrar un renglon, y no es que falte.
/// </summary>
[ApiController]
[Authorize]
[HasPermission(PermissionCatalog.AuditLogView)]
public sealed class AuditController(
    IMediator mediator,
    ResultViewModel<AuditController> viewModel) : BaseApiController(mediator)
{
    /// <summary>
    /// Que se hizo. Paginado en el servidor, con el total del conjunto filtrado.
    ///
    /// El filtro onlyPendingExternalEffect es el que mas se usa: son los renglones descuadrados,
    /// los que quedaron a medias con un sistema externo, y tienen su propio indice filtrado.
    /// </summary>
    [HttpGet("/api/audit/actions")]
    public async Task<IActionResult> GetActions(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string? actionCode,
        [FromQuery] string? subjectKey,
        [FromQuery] string? performedBy,
        [FromQuery] bool onlyPendingExternalEffect = false,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var response = await DispatchAsync(
            new GetActionAuditRequest(
                fromUtc, toUtc, actionCode, subjectKey, performedBy,
                onlyPendingExternalEffect, pageNumber, pageSize),
            cancellationToken);

        return MapResult(response, viewModel);
    }

    /// <summary>
    /// Quien lo intento y no pudo. Con onlyWouldHaveBeenBlocked se obtiene la lista de trabajo
    /// para encender el enforcement: exactamente lo que se bloquearia si se encendiera hoy.
    /// </summary>
    [HttpGet("/api/audit/denials")]
    public async Task<IActionResult> GetDenials(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string? permissionCode,
        [FromQuery] string? attemptedBy,
        [FromQuery] bool onlyWouldHaveBeenBlocked = false,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var response = await DispatchAsync(
            new GetAccessDenialsRequest(
                fromUtc, toUtc, permissionCode, attemptedBy,
                onlyWouldHaveBeenBlocked, pageNumber, pageSize),
            cancellationToken);

        return MapResult(response, viewModel);
    }
}
