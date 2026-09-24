using Common.Messaging;
using Common.Results;
using Common.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LabelPrinting.Shared.Web;

/// <summary>
/// Base de todos los controllers. Hace dos cosas y ninguna es logica de negocio:
/// despachar el caso de uso y traducir su resultado a un codigo HTTP.
///
/// El controller NO serializa la respuesta del handler. La respuesta se PUBLICA, y el presenter
/// registrado para ese tipo es quien llena el view model. Si falta el presenter, el endpoint
/// responde el envelope vacio -isSuccess false, data null, message vacio- aunque el proyecto
/// compile y el SQL este bien. Es el error mas caro de esta arquitectura y no lo caza el
/// compilador: ver ADR-0006.
/// </summary>
[ApiController]
public abstract class BaseApiController(IMediator mediator) : ControllerBase
{
    protected IMediator Mediator { get; } = mediator;

    protected async Task<TResponse> DispatchAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken)
        where TResponse : IResponse
    {
        var response = await Mediator.Send(request, cancellationToken).ConfigureAwait(false);
        await Mediator.Publish(response, cancellationToken).ConfigureAwait(false);
        return response;
    }

    protected IActionResult MapResult<TController>(object response, ResultViewModel<TController> viewModel) => response switch
    {
        IValidationFailure => BadRequest(viewModel),
        INotFoundFailure => NotFound(viewModel),
        IConflictFailure => Conflict(viewModel),
        IFailure => StatusCode(StatusCodes.Status500InternalServerError, viewModel),
        _ => Ok(viewModel),
    };
}
