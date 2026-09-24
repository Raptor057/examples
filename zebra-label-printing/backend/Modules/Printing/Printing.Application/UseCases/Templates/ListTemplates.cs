using Common.Messaging;
using Common.Results;
using Printing.Application.Dtos;
using Printing.Domain.Abstractions;

namespace Printing.Application.UseCases.Templates;

public sealed record ListTemplatesRequest(int? Dpi = null, bool? IsActive = true) : IRequest<ListTemplatesResponse>;

public abstract record ListTemplatesResponse : IResponse;

public sealed record ListTemplatesSuccess(IReadOnlyList<TemplateDto> Data)
    : ListTemplatesResponse, ISuccess<IReadOnlyList<TemplateDto>>;

internal sealed class ListTemplatesHandler(ILabelTemplateRepository repository)
    : IRequestHandler<ListTemplatesRequest, ListTemplatesResponse>
{
    public async Task<ListTemplatesResponse> Handle(ListTemplatesRequest request, CancellationToken cancellationToken)
    {
        var templates = await repository.ListAsync(request.Dpi, request.IsActive, cancellationToken).ConfigureAwait(false);
        return new ListTemplatesSuccess([.. templates.Select(t => t.ToDto())]);
    }
}
