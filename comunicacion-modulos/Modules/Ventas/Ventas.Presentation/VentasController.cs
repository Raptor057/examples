using Common.Messaging;
using Common.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Shared.Web;
using Ventas.Application.UseCases.ListarVentas;
using Ventas.Application.UseCases.RegistrarVenta;

namespace Ventas.Presentation;

[Route("api/ventas")]
public sealed class VentasController : BaseApiController
{
    private readonly ResultViewModel<VentasController> _vm;
    private readonly VentasResponseState _state;

    public VentasController(IMediator mediator, ResultViewModel<VentasController> vm, VentasResponseState state)
        : base(mediator)
    {
        _vm = vm;
        _state = state;
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarVentaBody body, CancellationToken ct = default)
    {
        _ = await Mediator.Send(new RegistrarVentaRequest(body.ProductoId, body.Cantidad, body.PaisCliente), ct);
        return _vm.IsSuccess ? Ok(_vm) : StatusCode(_state.HttpStatusCode, _vm);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct = default)
    {
        _ = await Mediator.Send(new ListarVentasRequest(), ct);
        return Ok(_vm);
    }
}

// PaisCliente se valida contra el global Geo; el vendedor lo pone el global Identity
// (header X-User-Name), no viaja en el body.
public sealed record RegistrarVentaBody(long ProductoId, int Cantidad, string PaisCliente);

public sealed class VentasResponseState
{
    public int HttpStatusCode { get; set; } = 400;
}
