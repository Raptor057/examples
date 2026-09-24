using Common.Messaging;
using Common.Results;
using Printing.Domain.Abstractions;
using Printing.Domain.Rules;

namespace Printing.Application.UseCases.Templates;

/// <summary>
/// Ver la etiqueta dibujada, con sus valores, sin gastar papel. Renderiza EXACTAMENTE igual que al
/// imprimir -mismo escapado incluido-, porque una vista previa que no coincide con lo que sale es
/// peor que no tenerla.
/// </summary>
public sealed record PreviewTemplateRequest(
    string? Code,
    int Dpi,
    IReadOnlyDictionary<string, string?>? Values) : IRequest<PreviewTemplateResponse>;

public abstract record PreviewTemplateResponse : IResponse;

public sealed record PreviewTemplateSuccess(LabelPreview Data)
    : PreviewTemplateResponse, ISuccess<LabelPreview>;

public sealed record PreviewTemplateNotFound(string Message) : PreviewTemplateResponse, INotFoundFailure;

public sealed record PreviewTemplateUnavailable(string Message) : PreviewTemplateResponse, IConflictFailure;

internal sealed class PreviewTemplateHandler(
    ILabelTemplateRepository repository,
    ILabelPreviewRenderer renderer) : IRequestHandler<PreviewTemplateRequest, PreviewTemplateResponse>
{
    public async Task<PreviewTemplateResponse> Handle(PreviewTemplateRequest request, CancellationToken cancellationToken)
    {
        var code = TemplateRules.NormalizeCode(request.Code);
        var template = await repository.FindAsync(code, request.Dpi, cancellationToken).ConfigureAwait(false);
        if (template is null)
            return new PreviewTemplateNotFound($"No existe la plantilla {code} para {request.Dpi} dpi.");

        var values = (request.Values ?? new Dictionary<string, string?>())
            .ToDictionary(p => p.Key.Trim().ToUpperInvariant(), p => p.Value, StringComparer.Ordinal);

        // El MISMO render que usa la impresion. Si aqui se hiciera una version "parecida", la
        // vista previa mentiria justo en los casos raros, que son los que se quieren ver.
        var zpl = TemplateRules.Render(template.Body, values);

        try
        {
            return new PreviewTemplateSuccess(await renderer.RenderAsync(zpl, template.Dpi, cancellationToken).ConfigureAwait(false));
        }
        catch (PreviewUnavailableException ex)
        {
            return new PreviewTemplateUnavailable(ex.Message);
        }
    }
}
