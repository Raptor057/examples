using Common.Messaging;
using Common.ViewModels;
using LabelPrinting.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Printing.Application.UseCases.Queue;

namespace Printing.Presentation.Controllers;

/// <summary>
/// La cola de impresion: que hay esperando, que murio, y sacar de la fila lo que ya no interesa.
///
/// Es la pantalla que se abre cuando una impresora estuvo caida: sin ella, nadie sabe cuantas
/// etiquetas van a salir de golpe al encenderla.
/// </summary>
[Route("api/print/queue")]
[Authorize(Policy = PrintingPermissions.ManageQueue)]
public sealed class PrintQueueController(IMediator mediator, ResultViewModel<PrintQueueController> viewModel)
    : BaseApiController(mediator)
{
    /// <param name="status">pending, sent, dead o cancelled. Omitido = todos.</param>
    [HttpGet]
    public async Task<IActionResult> List(string? status = null, int take = 50, CancellationToken cancellationToken = default)
        => MapResult(await DispatchAsync(new ListPrintQueueRequest(status, take), cancellationToken), viewModel);

    /// <summary>
    /// Saca un trabajo de la fila. Hace falta mas de lo que parece: tras media hora de impresora
    /// caida, lo ultimo que alguien quiere es que al encenderla escupa cuarenta etiquetas viejas.
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Cancel(long id, CancellationToken cancellationToken = default)
        => MapResult(await DispatchAsync(new CancelPrintQueueItemRequest(id), cancellationToken), viewModel);
}

/// <summary>
/// Los nombres de las politicas, en la capa que los usa. Duplican las constantes del catalogo del
/// Host a proposito: Presentation no puede referenciar al Host -seria una dependencia al reves- y
/// una cadena magica repartida por los controllers seria peor. El Host las registra con ESTOS
/// nombres, y si alguna no coincide la autorizacion falla al arrancar, no en silencio.
/// </summary>
public static class PrintingPermissions
{
    public const string Print = "printing:print";
    public const string ManageTemplates = "printing:templates:manage";
    public const string ManageQueue = "printing:queue:manage";
}
