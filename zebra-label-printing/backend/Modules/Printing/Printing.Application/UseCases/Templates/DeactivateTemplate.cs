using Common.Messaging;
using Common.Results;
using Printing.Domain.Abstractions;
using Printing.Domain.Rules;

namespace Printing.Application.UseCases.Templates;

/// <summary>Baja logica. No hay borrado fisico: hay etiquetas ya impresas que apuntan a esta plantilla.</summary>
public sealed record DeactivateTemplateRequest(string? Code, int Dpi) : IRequest<DeactivateTemplateResponse>;

public abstract record DeactivateTemplateResponse : IResponse;

public sealed record DeactivateTemplateSuccess : DeactivateTemplateResponse, ISuccess;

public sealed record DeactivateTemplateNotFound(string Message) : DeactivateTemplateResponse, INotFoundFailure;

internal sealed class DeactivateTemplateHandler(ILabelTemplateRepository repository)
    : IRequestHandler<DeactivateTemplateRequest, DeactivateTemplateResponse>
{
    public async Task<DeactivateTemplateResponse> Handle(DeactivateTemplateRequest request, CancellationToken cancellationToken)
    {
        var code = TemplateRules.NormalizeCode(request.Code);
        var done = await repository.DeactivateAsync(code, request.Dpi, cancellationToken).ConfigureAwait(false);

        return done
            ? new DeactivateTemplateSuccess()
            : new DeactivateTemplateNotFound($"No existe la plantilla {code} para {request.Dpi} dpi.");
    }
}
