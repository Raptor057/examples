using Common.Messaging;
using Common.Results;
using Printing.Application.Dtos;
using Printing.Domain.Abstractions;
using Printing.Domain.Entities;
using Printing.Domain.Rules;

namespace Printing.Application.UseCases.Templates;

/// <summary>
/// Alta y edicion en UN solo caso de uso. Se parecen tanto -mismas reglas, mismo cuerpo- que
/// separarlos solo duplicaria la validacion y abriria la puerta a que se desincronicen.
/// </summary>
/// <param name="IsUpdate">true = editar la existente; false = crear. Lo decide el verbo HTTP.</param>
public sealed record SaveTemplateRequest(
    string? Code,
    string? Name,
    string? Description,
    string? Body,
    int Dpi,
    bool IsUpdate) : IRequest<SaveTemplateResponse>;

public abstract record SaveTemplateResponse : IResponse;

public sealed record SaveTemplateSuccess(TemplateDto Data) : SaveTemplateResponse, ISuccess<TemplateDto>;

public sealed record SaveTemplateInvalid(string Message) : SaveTemplateResponse, IValidationFailure;

public sealed record SaveTemplateNotFound(string Message) : SaveTemplateResponse, INotFoundFailure;

public sealed record SaveTemplateConflict(string Message) : SaveTemplateResponse, IConflictFailure;

internal sealed class SaveTemplateHandler(ILabelTemplateRepository repository, TimeProvider time)
    : IRequestHandler<SaveTemplateRequest, SaveTemplateResponse>
{
    public async Task<SaveTemplateResponse> Handle(SaveTemplateRequest request, CancellationToken cancellationToken)
    {
        var error = TemplateRules.Validate(request.Code, request.Name, request.Body, request.Dpi);
        if (error is not null) return new SaveTemplateInvalid(error);

        var code = TemplateRules.NormalizeCode(request.Code);
        var exists = await repository.ExistsAsync(code, request.Dpi, cancellationToken).ConfigureAwait(false);

        if (request.IsUpdate && !exists)
            return new SaveTemplateNotFound($"No existe la plantilla {code} para {request.Dpi} dpi.");

        if (!request.IsUpdate && exists)
            return new SaveTemplateConflict($"Ya existe la plantilla {code} para {request.Dpi} dpi. Editala en vez de crearla.");

        var now = time.GetUtcNow().UtcDateTime;
        var template = new LabelTemplate
        {
            Code = code,
            Name = request.Name!.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Body = request.Body!.Trim(),
            Dpi = request.Dpi,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = request.IsUpdate ? now : null,
        };

        if (request.IsUpdate)
        {
            await repository.UpdateAsync(template, cancellationToken).ConfigureAwait(false);
            var saved = await repository.FindAsync(code, request.Dpi, cancellationToken).ConfigureAwait(false);
            return new SaveTemplateSuccess(saved!.ToDto());
        }

        var created = await repository.CreateAsync(template, cancellationToken).ConfigureAwait(false);
        return new SaveTemplateSuccess(created.ToDto());
    }
}
