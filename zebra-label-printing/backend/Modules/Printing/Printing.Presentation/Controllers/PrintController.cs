using Common.Messaging;
using Common.ViewModels;
using LabelPrinting.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printing.Application.Dtos;
using Printing.Application.UseCases.Jobs;
using Printing.Application.UseCases.Printers;
using Printing.Application.UseCases.Templates;

namespace Printing.Presentation.Controllers;

[Route("api/print")]
[Authorize(Policy = PrintingPermissions.Print)]
public sealed class PrintController(IMediator mediator, ResultViewModel<PrintController> viewModel)
    : BaseApiController(mediator)
{
    /// <summary>
    /// ZPL escrito a mano. Exige el permiso de ADMINISTRAR plantillas, no el de imprimir: mandar
    /// ZPL crudo es mandarle cualquier comando a la impresora -reconfigurarla, borrar su memoria-,
    /// y eso no es lo mismo que pulsar "imprimir" en una etiqueta ya aprobada.
    /// </summary>
    [HttpPost("zpl")]
    [Authorize(Policy = PrintingPermissions.ManageTemplates)]
    public async Task<IActionResult> Raw([FromBody] SendRawZplBody body, CancellationToken cancellationToken = default)
        => MapResult(
            await DispatchAsync(new SendRawZplRequest(body.Target, body.Zpl, body.QueueOnFailure), cancellationToken),
            viewModel);

    [HttpPost("template")]
    public async Task<IActionResult> Template([FromBody] PrintTemplateBody body, CancellationToken cancellationToken = default)
        => MapResult(
            await DispatchAsync(
                new PrintTemplateRequest(body.Code, body.Dpi, body.Target, body.Values, body.QueueOnFailure, body.Version),
                cancellationToken),
            viewModel);

    [HttpGet("jobs")]
    public async Task<IActionResult> Jobs(int take = 50, CancellationToken cancellationToken = default)
        => MapResult(await DispatchAsync(new ListPrintJobsRequest(take), cancellationToken), viewModel);

    /// <summary>
    /// Los cuerpos de peticion viven en la capa de presentacion, no en Application: son el
    /// contrato HTTP, y el caso de uso no tiene por que cambiar si manana el cliente manda los
    /// mismos datos con otra forma.
    /// </summary>
    public sealed record SendRawZplBody(PrinterTargetDto? Target, string? Zpl, bool QueueOnFailure = true);

    /// <param name="QueueOnFailure">
    /// Si la impresora no contesta, encolar en vez de rendirse. Por omision SI: perder una
    /// etiqueta porque la impresora estaba apagada un minuto es el fallo que la cola evita.
    /// </param>
    /// <param name="Version">Version concreta de la plantilla, para reimprimir lo de entonces.</param>
    public sealed record PrintTemplateBody(
        string? Code,
        int Dpi,
        PrinterTargetDto? Target,
        Dictionary<string, string?>? Values,
        bool QueueOnFailure = true,
        int? Version = null);
}
