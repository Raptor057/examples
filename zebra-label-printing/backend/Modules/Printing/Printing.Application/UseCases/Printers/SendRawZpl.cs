using Common.Messaging;
using Common.Results;
using Printing.Application.Dtos;
using Printing.Application.Services;
using Printing.Domain.Entities;

namespace Printing.Application.UseCases.Printers;

/// <summary>Mandar ZPL escrito a mano. Es la valvula de escape para diagnosticar sin plantillas.</summary>
public sealed record SendRawZplRequest(PrinterTargetDto? Target, string? Zpl, bool QueueOnFailure = true)
    : IRequest<SendRawZplResponse>;

public abstract record SendRawZplResponse : IResponse;

public sealed record SendRawZplSuccess(TemplatePrintResultDto Data)
    : SendRawZplResponse, ISuccess<TemplatePrintResultDto>;

public sealed record SendRawZplInvalid(string Message) : SendRawZplResponse, IValidationFailure;

public sealed record SendRawZplUnreachable(string Message) : SendRawZplResponse, IConflictFailure;

internal sealed class SendRawZplHandler(PrintDispatchService dispatch)
    : IRequestHandler<SendRawZplRequest, SendRawZplResponse>
{
    public async Task<SendRawZplResponse> Handle(SendRawZplRequest request, CancellationToken cancellationToken)
    {
        var (target, error) = request.Target.ToDomain();
        if (target is null) return new SendRawZplInvalid(error!);

        var zpl = (request.Zpl ?? string.Empty).Trim();
        if (zpl.Length == 0) return new SendRawZplInvalid("El ZPL viene vacio.");

        // Sin preflight: este endpoint existe para diagnosticar, y a veces lo que se quiere es
        // justo mandarle algo a una impresora que reporta problemas.
        var result = await dispatch.SendAsync(
            target, zpl, null, null, null, request.QueueOnFailure, preflight: false, cancellationToken)
            .ConfigureAwait(false);

        if (result.Disposition == PrintDisposition.Rejected)
            return new SendRawZplUnreachable(result.Message!);

        return new SendRawZplSuccess(new TemplatePrintResultDto(
            result.Receipt?.ToDto(), null, null, null, [],
            result.Disposition.ToString().ToLowerInvariant(), result.QueueItemId, result.Message));
    }
}
