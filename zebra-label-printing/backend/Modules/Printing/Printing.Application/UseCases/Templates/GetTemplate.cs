using Common.Messaging;
using Common.Results;
using Printing.Application.Dtos;
using Printing.Domain.Abstractions;
using Printing.Domain.Rules;

namespace Printing.Application.UseCases.Templates;

public sealed record GetTemplateRequest(string? Code, int Dpi) : IRequest<GetTemplateResponse>;

public abstract record GetTemplateResponse : IResponse;

public sealed record GetTemplateSuccess(TemplateDto Data) : GetTemplateResponse, ISuccess<TemplateDto>;

public sealed record GetTemplateNotFound(string Message) : GetTemplateResponse, INotFoundFailure;

internal sealed class GetTemplateHandler(ILabelTemplateRepository repository)
    : IRequestHandler<GetTemplateRequest, GetTemplateResponse>
{
    public async Task<GetTemplateResponse> Handle(GetTemplateRequest request, CancellationToken cancellationToken)
    {
        var code = TemplateRules.NormalizeCode(request.Code);
        var template = await repository.FindAsync(code, request.Dpi, cancellationToken).ConfigureAwait(false);

        // El mensaje nombra las DOS partes de la llave. "No existe BOX_LABEL" manda a buscar un
        // problema que no es: la plantilla puede existir, pero no para esa resolucion.
        return template is null
            ? new GetTemplateNotFound($"No existe la plantilla {code} para {request.Dpi} dpi.")
            : new GetTemplateSuccess(template.ToDto());
    }
}
