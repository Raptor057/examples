using Common.Messaging;
using Common.Results;
using Printing.Application.Dtos;
using Printing.Domain.Abstractions;

namespace Printing.Application.UseCases.Jobs;

/// <summary>Lo ultimo que se mando a imprimir, con su ZPL exacto. Es la pantalla de diagnostico.</summary>
public sealed record ListPrintJobsRequest(int Take = 50) : IRequest<ListPrintJobsResponse>;

public abstract record ListPrintJobsResponse : IResponse;

public sealed record ListPrintJobsSuccess(IReadOnlyList<PrintJobDto> Data)
    : ListPrintJobsResponse, ISuccess<IReadOnlyList<PrintJobDto>>;

internal sealed class ListPrintJobsHandler(IPrintJobLog jobLog)
    : IRequestHandler<ListPrintJobsRequest, ListPrintJobsResponse>
{
    /// <summary>Tope duro: sin el, un cliente pide 100000 y se trae el ZPL de cada uno.</summary>
    private const int MaxTake = 200;

    public async Task<ListPrintJobsResponse> Handle(ListPrintJobsRequest request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, MaxTake);
        var entries = await jobLog.RecentAsync(take, cancellationToken).ConfigureAwait(false);
        return new ListPrintJobsSuccess([.. entries.Select(e => e.ToDto())]);
    }
}
