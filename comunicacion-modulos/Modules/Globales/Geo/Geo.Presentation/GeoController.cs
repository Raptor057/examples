using Common.Messaging;
using Common.ViewModels;
using Geo.Application.UseCases.ListarPaises;
using Microsoft.AspNetCore.Mvc;
using Shared.Web;

namespace Geo.Presentation;

[Route("api/geo")]
public sealed class GeoController : BaseApiController
{
    private readonly ResultViewModel<GeoController> _vm;

    public GeoController(IMediator mediator, ResultViewModel<GeoController> vm)
        : base(mediator) => _vm = vm;

    [HttpGet("paises")]
    public async Task<IActionResult> ListarPaises(CancellationToken ct = default)
    {
        _ = await Mediator.Send(new ListarPaisesRequest(), ct);
        return _vm.IsSuccess ? Ok(_vm) : StatusCode(400, _vm);
    }
}
