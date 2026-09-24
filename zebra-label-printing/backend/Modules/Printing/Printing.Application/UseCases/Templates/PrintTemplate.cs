using Common.Messaging;
using Common.Results;
using Printing.Application.Dtos;
using Printing.Application.Services;
using Printing.Domain.Abstractions;
using Printing.Domain.Entities;
using Printing.Domain.Rules;

namespace Printing.Application.UseCases.Templates;

/// <summary>
/// El caso de uso central: agarra una plantilla, le mete los valores y la manda a imprimir.
/// Es el unico sitio donde se juntan las tres piezas -plantilla, valores e impresora-.
/// </summary>
/// <param name="Version">
/// Version concreta del cuerpo, para reimprimir exactamente lo que se imprimio en su momento.
/// Nulo = la vigente, que es lo normal.
/// </param>
public sealed record PrintTemplateRequest(
    string? Code,
    int Dpi,
    PrinterTargetDto? Target,
    IReadOnlyDictionary<string, string?>? Values,
    bool QueueOnFailure = true,
    int? Version = null) : IRequest<PrintTemplateResponse>;

public abstract record PrintTemplateResponse : IResponse;

public sealed record PrintTemplateSuccess(TemplatePrintResultDto Data)
    : PrintTemplateResponse, ISuccess<TemplatePrintResultDto>;

public sealed record PrintTemplateInvalid(string Message) : PrintTemplateResponse, IValidationFailure;

public sealed record PrintTemplateNotFound(string Message) : PrintTemplateResponse, INotFoundFailure;

public sealed record PrintTemplateUnreachable(string Message) : PrintTemplateResponse, IConflictFailure;

internal sealed class PrintTemplateHandler(
    ILabelTemplateRepository repository,
    PrintDispatchService dispatch) : IRequestHandler<PrintTemplateRequest, PrintTemplateResponse>
{
    public async Task<PrintTemplateResponse> Handle(PrintTemplateRequest request, CancellationToken cancellationToken)
    {
        var (target, targetError) = request.Target.ToDomain();
        if (target is null) return new PrintTemplateInvalid(targetError!);

        var code = TemplateRules.NormalizeCode(request.Code);
        if (code.Length == 0) return new PrintTemplateInvalid("Falta el codigo de la plantilla.");

        var template = await repository.FindAsync(code, request.Dpi, cancellationToken).ConfigureAwait(false);
        if (template is null)
            return new PrintTemplateNotFound($"No existe la plantilla {code} para {request.Dpi} dpi.");

        if (!template.IsActive)
            return new PrintTemplateInvalid($"La plantilla {code} ({request.Dpi} dpi) esta dada de baja.");

        // Reimpresion historica: se pide una version concreta y se usa SU cuerpo, no el de hoy.
        var body = template.Body;
        var version = template.Version;
        if (request.Version is int pedida && pedida != template.Version)
        {
            var historica = await repository.FindVersionAsync(code, request.Dpi, pedida, cancellationToken).ConfigureAwait(false);
            if (historica is null)
                return new PrintTemplateNotFound($"La plantilla {code} ({request.Dpi} dpi) no tiene version {pedida}.");
            body = historica.Body;
            version = historica.Version;
        }

        // Las llaves llegan como las escribio el cliente; los marcadores son mayusculas. Se
        // normaliza aqui y no en el dominio para que la regla de render siga siendo pura.
        var values = (request.Values ?? new Dictionary<string, string?>())
            .ToDictionary(pair => pair.Key.Trim().ToUpperInvariant(), pair => pair.Value, StringComparer.Ordinal);

        // Se calcula ANTES de imprimir para poder devolverlo aunque la impresion salga bien: es
        // una advertencia sobre el resultado, no un motivo para no imprimir.
        var missing = TemplateRules.MissingValues(body, values);
        var zpl = TemplateRules.Render(body, values);

        var result = await dispatch.SendAsync(
            target, zpl, template.Code, template.Dpi, version,
            request.QueueOnFailure, preflight: true, cancellationToken).ConfigureAwait(false);

        // Rechazado es lo unico que NO sale: encolado si salio del cliente y va a reintentarse.
        if (result.Disposition == PrintDisposition.Rejected)
            return new PrintTemplateUnreachable(result.Message!);

        return new PrintTemplateSuccess(new TemplatePrintResultDto(
            result.Receipt?.ToDto(),
            template.Code,
            template.Dpi,
            version,
            missing,
            result.Disposition.ToString().ToLowerInvariant(),
            result.QueueItemId,
            result.Message));
    }
}
