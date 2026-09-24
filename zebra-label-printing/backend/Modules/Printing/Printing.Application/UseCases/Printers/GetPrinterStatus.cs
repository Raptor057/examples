using Common.Messaging;
using Common.Results;
using Printing.Application.Dtos;
using Printing.Domain.Abstractions;

namespace Printing.Application.UseCases.Printers;

public sealed record GetPrinterStatusRequest(PrinterTargetDto? Target) : IRequest<GetPrinterStatusResponse>;

public abstract record GetPrinterStatusResponse : IResponse;

public sealed record GetPrinterStatusSuccess(PrinterStatusDto Data)
    : GetPrinterStatusResponse, ISuccess<PrinterStatusDto>;

public sealed record GetPrinterStatusInvalid(string Message) : GetPrinterStatusResponse, IValidationFailure;

public sealed record GetPrinterStatusUnreachable(string Message) : GetPrinterStatusResponse, IConflictFailure;

internal sealed class GetPrinterStatusHandler(IPrinterGateway gateway)
    : IRequestHandler<GetPrinterStatusRequest, GetPrinterStatusResponse>
{
    public async Task<GetPrinterStatusResponse> Handle(GetPrinterStatusRequest request, CancellationToken cancellationToken)
    {
        var (target, error) = request.Target.ToDomain();
        if (target is null) return new GetPrinterStatusInvalid(error!);

        try
        {
            var status = await gateway.GetStatusAsync(target, cancellationToken).ConfigureAwait(false);
            return new GetPrinterStatusSuccess(status.ToDto());
        }
        catch (PrinterCommunicationException ex)
        {
            // Que una impresora no conteste no es un error del servidor: es el estado normal de
            // una impresora apagada. Viaja como 409 y con el mensaje que vio el adaptador.
            return new GetPrinterStatusUnreachable(ex.Message);
        }
    }
}
