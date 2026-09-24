using Common.Messaging;
using Common.ViewModels;
using LabelPrinting.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printing.Application.UseCases.Templates;

namespace Printing.Presentation.Controllers;

[Route("api/templates")]
[Authorize(Policy = PrintingPermissions.Print)]
public sealed class TemplatesController(IMediator mediator, ResultViewModel<TemplatesController> viewModel)
    : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> List(int? dpi = null, bool? isActive = true, CancellationToken cancellationToken = default)
        => MapResult(await DispatchAsync(new ListTemplatesRequest(dpi, isActive), cancellationToken), viewModel);

    /// <summary>El dpi es obligatorio: el codigo por si solo no identifica una plantilla.</summary>
    [HttpGet("{code}")]
    public async Task<IActionResult> Get(string code, [FromQuery] int dpi, CancellationToken cancellationToken = default)
        => MapResult(await DispatchAsync(new GetTemplateRequest(code, dpi), cancellationToken), viewModel);

    [HttpPost]
    [Authorize(Policy = PrintingPermissions.ManageTemplates)]
    public async Task<IActionResult> Create([FromBody] TemplateBody body, CancellationToken cancellationToken = default)
        => MapResult(
            await DispatchAsync(
                new SaveTemplateRequest(body.Code, body.Name, body.Description, body.Body, body.Dpi, IsUpdate: false),
                cancellationToken),
            viewModel);

    [HttpPut("{code}")]
    [Authorize(Policy = PrintingPermissions.ManageTemplates)]
    public async Task<IActionResult> Update(string code, [FromBody] TemplateBody body, CancellationToken cancellationToken = default)
        => MapResult(
            await DispatchAsync(
                new SaveTemplateRequest(code, body.Name, body.Description, body.Body, body.Dpi, IsUpdate: true),
                cancellationToken),
            viewModel);

    [HttpDelete("{code}")]
    [Authorize(Policy = PrintingPermissions.ManageTemplates)]
    public async Task<IActionResult> Deactivate(string code, [FromQuery] int dpi, CancellationToken cancellationToken = default)
        => MapResult(await DispatchAsync(new DeactivateTemplateRequest(code, dpi), cancellationToken), viewModel);

    /// <summary>El historial de versiones de una plantilla.</summary>
    [HttpGet("{code}/versions")]
    public async Task<IActionResult> History(string code, [FromQuery] int dpi, CancellationToken cancellationToken = default)
        => MapResult(await DispatchAsync(new GetTemplateHistoryRequest(code, dpi), cancellationToken), viewModel);

    /// <summary>
    /// La etiqueta dibujada, sin gastar papel. Devuelve PNG y NO el envelope: es una imagen, y
    /// envolverla en JSON obligaria a que el cliente la decodificara en base64 para nada.
    ///
    /// Puede estar apagada: manda el contenido de la etiqueta a un servicio de terceros.
    /// </summary>
    [HttpPost("{code}/preview")]
    public async Task<IActionResult> Preview(
        string code,
        [FromQuery] int dpi,
        [FromBody] Dictionary<string, string?>? values,
        CancellationToken cancellationToken = default)
    {
        var response = await DispatchAsync(new PreviewTemplateRequest(code, dpi, values), cancellationToken);
        return response switch
        {
            PreviewTemplateSuccess ok => File(ok.Data.Content, ok.Data.ContentType),
            _ => MapResult(response, viewModel),
        };
    }

    public sealed record TemplateBody(string? Code, string? Name, string? Description, string? Body, int Dpi);
}
