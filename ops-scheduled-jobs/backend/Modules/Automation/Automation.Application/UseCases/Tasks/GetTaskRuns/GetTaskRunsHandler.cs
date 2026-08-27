using Automation.Application.Dtos;
using Automation.Application.UseCases.Tasks.GetTaskRuns.Responses;
using Automation.Domain.Repositories;
using Automation.Domain.Tasks;
using Common.Messaging;

namespace Automation.Application.UseCases.Tasks.GetTaskRuns;

/// <summary>
/// Bitacora paginada. Filtrar, ordenar y paginar son la misma operacion y se resuelven en el
/// mismo sitio: el servidor. Aqui solo se validan los criterios que llegan del cliente.
/// </summary>
internal sealed class GetTaskRunsHandler(IAutomatedTaskRunRepository runs)
    : IRequestHandler<GetTaskRunsRequest, GetTaskRunsResponse>
{
    private const int DefaultPageSize = 20;

    /// <summary>
    /// Tope al tamano de pagina. Sin el, un cliente pide 1,000,000 y la "paginacion" deja de
    /// serlo: es un barrido con otro nombre.
    /// </summary>
    private const int MaxPageSize = 100;

    public async Task<GetTaskRunsResponse> Handle(GetTaskRunsRequest request, CancellationToken cancellationToken)
    {
        var code = (request.Code ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(code))
            return new GetTaskRunsValidationFailure("Falta el codigo de la tarea.");

        // El catalogo en codigo manda tambien para leer: si el codigo no esta declarado, no
        // existe, aunque alguien haya metido filas con ese TaskCode en la bitacora.
        if (!AutomatedTaskCatalog.Contains(code))
            return new GetTaskRunsNotFoundFailure($"No existe la tarea '{code}'.");

        var pageNumber = request.PageNumber is > 0 ? request.PageNumber.Value : 1;
        var pageSize = request.PageSize is > 0 ? request.PageSize.Value : DefaultPageSize;
        if (pageSize > MaxPageSize)
            return new GetTaskRunsValidationFailure($"El tamano de pagina maximo es {MaxPageSize}.");

        var page = await runs.GetPageAsync(code, pageNumber, pageSize, cancellationToken).ConfigureAwait(false);

        return new GetTaskRunsSuccess(new TaskRunPageDto(
            code,
            page.Items.Select(AutomationMapper.ToDto).ToList(),
            page.TotalCount,
            page.PageNumber,
            page.PageSize,
            page.HasMore));
    }
}
