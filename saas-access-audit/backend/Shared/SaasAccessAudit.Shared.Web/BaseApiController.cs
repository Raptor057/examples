using Common.Messaging;
using Common.Results;
using Common.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace SaasAccessAudit.Shared.Web;

/// <summary>
/// Base de todos los controllers. Despacha al caso de uso, publica la respuesta (lo que
/// dispara el presenter) y traduce el tipo de respuesta a codigo HTTP. El controller no
/// serializa nada por su cuenta: el cuerpo siempre es el envelope que lleno el presenter.
/// </summary>
[ApiController]
public abstract class BaseApiController(IMediator mediator) : ControllerBase
{
    protected IMediator Mediator { get; } = mediator;

    protected async Task<TResponse> DispatchAsync<TResponse>(
        IRequest<TResponse> request, CancellationToken cancellationToken)
        where TResponse : IResponse
    {
        var response = await Mediator.Send(request, cancellationToken).ConfigureAwait(false);

        // Publicar es lo que llena el view model. Si falta el presenter, el endpoint responde
        // el envelope vacio aunque el proyecto compile y el SQL sea correcto.
        await Mediator.Publish(response, cancellationToken).ConfigureAwait(false);
        return response;
    }

    protected IActionResult MapResult<TController>(IResponse response, ResultViewModel<TController> viewModel) =>
        response switch
        {
            INotFoundFailure => NotFound(viewModel),
            IConflictFailure => Conflict(viewModel),
            IValidationFailure => BadRequest(viewModel),
            // Fallo de negocio esperado: HTTP 200 con isSuccess = false. El cliente nunca
            // asume que 200 significa exito; lee isSuccess.
            _ => Ok(viewModel)
        };
}
