using Common.Messaging;
using Common.ViewModels;
using LabelPrinting.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printing.Application.Dtos;
using Printing.Application.UseCases.Printers;

namespace Printing.Presentation.Controllers;

[Route("api/printers")]
[Authorize(Policy = PrintingPermissions.Print)]
public sealed class PrintersController(IMediator mediator, ResultViewModel<PrintersController> viewModel)
    : BaseApiController(mediator)
{
    /// <param name="includeNetwork">Barre la red ademas de las colas locales. Tarda segundos.</param>
    [HttpGet]
    public async Task<IActionResult> List(bool includeNetwork = false, CancellationToken cancellationToken = default)
        => MapResult(await DispatchAsync(new ListPrintersRequest(includeNetwork), cancellationToken), viewModel);

    /// <summary>
    /// POST y no GET aunque solo lea: el destino es un objeto -transporte, direccion, puerto o
    /// cola- y meterlo en la query string obliga a inventar un formato y a escaparlo a mano.
    /// </summary>
    [HttpPost("status")]
    public async Task<IActionResult> Status([FromBody] PrinterTargetDto target, CancellationToken cancellationToken = default)
        => MapResult(await DispatchAsync(new GetPrinterStatusRequest(target), cancellationToken), viewModel);
}
