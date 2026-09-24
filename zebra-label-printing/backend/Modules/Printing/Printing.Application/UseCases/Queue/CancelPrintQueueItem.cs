using Common.Messaging;
using Common.Results;
using Printing.Domain.Abstractions;

namespace Printing.Application.UseCases.Queue;

/// <summary>
/// Sacar un trabajo de la cola. Hace falta mas de lo que parece: cuando una impresora estuvo caida
/// media hora, lo ultimo que alguien quiere es que al encenderla escupa cuarenta etiquetas viejas.
/// </summary>
public sealed record CancelPrintQueueItemRequest(long Id) : IRequest<CancelPrintQueueItemResponse>;

public abstract record CancelPrintQueueItemResponse : IResponse;

public sealed record CancelPrintQueueItemSuccess : CancelPrintQueueItemResponse, ISuccess;

public sealed record CancelPrintQueueItemNotFound(string Message) : CancelPrintQueueItemResponse, INotFoundFailure;

internal sealed class CancelPrintQueueItemHandler(IPrintQueue queue, TimeProvider time)
    : IRequestHandler<CancelPrintQueueItemRequest, CancelPrintQueueItemResponse>
{
    public async Task<CancelPrintQueueItemResponse> Handle(CancelPrintQueueItemRequest request, CancellationToken cancellationToken)
    {
        var done = await queue
            .CancelAsync(request.Id, time.GetUtcNow().UtcDateTime, cancellationToken)
            .ConfigureAwait(false);

        // Si no estaba pendiente, no hay nada que cancelar: o ya salio, o ya estaba muerto.
        return done
            ? new CancelPrintQueueItemSuccess()
            : new CancelPrintQueueItemNotFound($"El trabajo {request.Id} no esta pendiente: ya salio, se cancelo o se agotaron sus intentos.");
    }
}
