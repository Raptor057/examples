using Common.Messaging;
using Common.Results;
using Printing.Application.Dtos;
using Printing.Domain.Abstractions;
using Printing.Domain.Rules;

namespace Printing.Application.UseCases.Templates;

/// <summary>
/// El historial de una plantilla. Contesta "con que etiqueta se imprimio esto en julio", que sin
/// versionado no tiene respuesta.
/// </summary>
public sealed record GetTemplateHistoryRequest(string? Code, int Dpi) : IRequest<GetTemplateHistoryResponse>;

public abstract record GetTemplateHistoryResponse : IResponse;

public sealed record GetTemplateHistorySuccess(IReadOnlyList<TemplateVersionDto> Data)
    : GetTemplateHistoryResponse, ISuccess<IReadOnlyList<TemplateVersionDto>>;

internal sealed class GetTemplateHistoryHandler(ILabelTemplateRepository repository)
    : IRequestHandler<GetTemplateHistoryRequest, GetTemplateHistoryResponse>
{
    public async Task<GetTemplateHistoryResponse> Handle(GetTemplateHistoryRequest request, CancellationToken cancellationToken)
    {
        var code = TemplateRules.NormalizeCode(request.Code);
        var versions = await repository.HistoryAsync(code, request.Dpi, cancellationToken).ConfigureAwait(false);

        return new GetTemplateHistorySuccess([.. versions.Select(v =>
            new TemplateVersionDto(v.Version, v.Name, v.Description, v.Body, v.CreatedAtUtc, v.ReplacedAtUtc))]);
    }
}
