using Access.Application.UseCases.DeactivateUser;
using Access.Application.UseCases.GetMyAccess;
using Access.Application.UseCases.GetRoles;
using Access.Application.UseCases.GetUsers;
using Access.Application.UseCases.SetRolePermission;
using Access.Contracts;
using Common.Messaging;
using Common.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaasAccessAudit.Shared.Web;
using SaasAccessAudit.Shared.Web.Authorization;

namespace Access.Presentation.Controllers;

public sealed record DeactivateUserBody(string? Reason);

public sealed record SetRolePermissionBody(string? PermissionCode, bool Granted, string? Reason);

/// <summary>
/// LA POLITICA SE DECLARA AQUI, SOBRE EL ENDPOINT, Y ES EL UNICO PUNTO QUE CUENTA.
///
/// Lo que el frontend esconda es cortesia. Este atributo es la seguridad: un cliente que llame la
/// ruta a mano se topa con el mismo 403 aunque nunca haya visto el boton.
///
/// El codigo del permiso viene de la constante del catalogo, no de un literal. Un typo en un
/// literal se ve exactamente igual que "esta persona no tiene el permiso", y es el punto 6 de la
/// lista de diagnostico de un 403; con la constante lo atrapa el compilador. Ademas hay una
/// prueba que recorre estos endpoints y falla si alguno exige un permiso que el catalogo no
/// declara.
/// </summary>
[ApiController]
[Authorize]
public sealed class AccessController(
    IMediator mediator,
    ResultViewModel<AccessController> viewModel) : BaseApiController(mediator)
{
    /// <summary>
    /// LO PROPIO NO LLEVA PERMISO. Consultar tu identidad, tus roles y tus permisos lo puede
    /// hacer cualquiera que este autenticado: exigir un permiso aqui dejaria a la aplicacion sin
    /// forma de saber que esconder. El filtro que garantiza que solo ves lo tuyo es el token.
    /// </summary>
    [HttpGet("/api/access/me")]
    public async Task<IActionResult> GetMyAccess(CancellationToken cancellationToken = default)
    {
        var response = await DispatchAsync(new GetMyAccessRequest(), cancellationToken);
        return MapResult(response, viewModel);
    }

    [HttpGet("/api/access/users")]
    [HasPermission(PermissionCatalog.UsersView)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] bool onlyActive = false,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var response = await DispatchAsync(
            new GetUsersRequest(search, onlyActive, pageNumber, pageSize), cancellationToken);

        return MapResult(response, viewModel);
    }

    /// <summary>
    /// La accion destructiva. Tres candados, y ninguno sustituye a los otros:
    ///   1. permiso PROPIO -no el manage del modulo-, que es este atributo,
    ///   2. motivo obligatorio, que valida el handler y defiende un CHECK en la base,
    ///   3. bitacora, que escribe el handler despues de ejecutar.
    ///
    /// El dialogo de confirmacion del frontend NO es un cuarto candado: lo pulsa cualquiera que
    /// haya llegado a la pantalla y no deja rastro. Avisa, no autoriza.
    /// </summary>
    [HttpPost("/api/access/users/{publicId:guid}/deactivate")]
    [HasPermission(PermissionCatalog.UsersDeactivate)]
    public async Task<IActionResult> DeactivateUser(
        Guid publicId,
        [FromBody] DeactivateUserBody body,
        CancellationToken cancellationToken = default)
    {
        var response = await DispatchAsync(
            new DeactivateUserRequest(publicId, body.Reason), cancellationToken);

        return MapResult(response, viewModel);
    }

    [HttpGet("/api/access/roles")]
    [HasPermission(PermissionCatalog.RolesView)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken = default)
    {
        var response = await DispatchAsync(new GetRolesRequest(), cancellationToken);
        return MapResult(response, viewModel);
    }

    /// <summary>
    /// Conceder o revocar. No borra un dato y aun asi lleva permiso propio, motivo y bitacora:
    /// cambia lo que OTRAS personas pueden hacer, y eso la regla lo lista explicitamente entre lo
    /// que se audita.
    /// </summary>
    [HttpPost("/api/access/roles/{publicId:guid}/permissions")]
    [HasPermission(PermissionCatalog.RolesManage)]
    public async Task<IActionResult> SetRolePermission(
        Guid publicId,
        [FromBody] SetRolePermissionBody body,
        CancellationToken cancellationToken = default)
    {
        var response = await DispatchAsync(
            new SetRolePermissionRequest(publicId, body.PermissionCode, body.Granted, body.Reason),
            cancellationToken);

        return MapResult(response, viewModel);
    }
}
