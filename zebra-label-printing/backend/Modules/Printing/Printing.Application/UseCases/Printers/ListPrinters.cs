using Common.Messaging;
using Common.Results;
using Printing.Application.Dtos;
using Printing.Domain.Abstractions;

namespace Printing.Application.UseCases.Printers;

/// <param name="IncludeNetwork">
/// Barrer la red es lento -segundos- y ruidoso: manda paquetes de descubrimiento a toda la subred.
/// Por eso NO es el comportamiento por omision; se pide explicitamente.
/// </param>
public sealed record ListPrintersRequest(bool IncludeNetwork = false) : IRequest<ListPrintersResponse>;

public abstract record ListPrintersResponse : IResponse;

public sealed record ListPrintersSuccess(IReadOnlyList<PrinterDto> Data)
    : ListPrintersResponse, ISuccess<IReadOnlyList<PrinterDto>>;

public sealed record ListPrintersUnavailable(string Message) : ListPrintersResponse, IConflictFailure;

internal sealed class ListPrintersHandler(IPrinterGateway gateway)
    : IRequestHandler<ListPrintersRequest, ListPrintersResponse>
{
    public async Task<ListPrintersResponse> Handle(ListPrintersRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var printers = await gateway.DiscoverAsync(request.IncludeNetwork, cancellationToken).ConfigureAwait(false);
            return new ListPrintersSuccess([.. printers.Select(p => p.ToDto())]);
        }
        catch (PrinterCommunicationException ex)
        {
            return new ListPrintersUnavailable(ex.Message);
        }
    }
}
