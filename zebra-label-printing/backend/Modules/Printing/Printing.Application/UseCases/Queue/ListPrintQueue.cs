using Common.Messaging;
using Common.Results;
using Printing.Application.Dtos;
using Printing.Domain.Abstractions;
using Printing.Domain.Entities;

namespace Printing.Application.UseCases.Queue;

public sealed record ListPrintQueueRequest(string? Status = null, int Take = 50) : IRequest<ListPrintQueueResponse>;

public abstract record ListPrintQueueResponse : IResponse;

public sealed record ListPrintQueueSuccess(IReadOnlyList<PrintQueueItemDto> Data)
    : ListPrintQueueResponse, ISuccess<IReadOnlyList<PrintQueueItemDto>>;

public sealed record ListPrintQueueInvalid(string Message) : ListPrintQueueResponse, IValidationFailure;

internal sealed class ListPrintQueueHandler(IPrintQueue queue)
    : IRequestHandler<ListPrintQueueRequest, ListPrintQueueResponse>
{
    private const int MaxTake = 200;

    public async Task<ListPrintQueueResponse> Handle(ListPrintQueueRequest request, CancellationToken cancellationToken)
    {
        PrintQueueStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<PrintQueueStatus>(request.Status, ignoreCase: true, out var parsed))
                return new ListPrintQueueInvalid("El estado debe ser pending, sent, dead o cancelled.");
            status = parsed;
        }

        var items = await queue
            .ListAsync(status, Math.Clamp(request.Take, 1, MaxTake), cancellationToken)
            .ConfigureAwait(false);

        return new ListPrintQueueSuccess([.. items.Select(i => new PrintQueueItemDto(
            i.Id, i.TargetLabel, i.TemplateCode, i.Dpi,
            i.Status.ToString().ToLowerInvariant(), i.Attempts, i.NextAttemptAtUtc, i.LastError, i.CreatedAtUtc))]);
    }
}
