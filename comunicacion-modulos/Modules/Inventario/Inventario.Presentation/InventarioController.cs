using Common.Messaging;
using Common.ViewModels;
using Inventario.Application.UseCases.ListarProductos;
using Microsoft.AspNetCore.Mvc;
using Shared.Web;

namespace Inventario.Presentation;

// Igual que AuthController en ArccNova: hereda BaseApiController, envia el Request
// por el Mediator y devuelve el ResultViewModel que llena el Presenter.
[Route("api/inventario")]
public sealed class InventarioController : BaseApiController
{
    private readonly ResultViewModel<InventarioController> _vm;

    public InventarioController(IMediator mediator, ResultViewModel<InventarioController> vm)
        : base(mediator) => _vm = vm;

    [HttpGet("productos")]
    public async Task<IActionResult> Listar(CancellationToken ct = default)
    {
        _ = await Mediator.Send(new ListarProductosRequest(), ct);
        return _vm.IsSuccess ? Ok(_vm) : StatusCode(400, _vm);
    }
}
