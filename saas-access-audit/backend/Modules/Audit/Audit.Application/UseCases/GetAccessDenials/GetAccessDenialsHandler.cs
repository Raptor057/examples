using Audit.Application.Dtos;
using Audit.Application.UseCases.GetAccessDenials.Responses;
using Audit.Domain.Models;
using Audit.Domain.Repositories;
using Common.Messaging;
using SaasAccessAudit.Kernel;

namespace Audit.Application.UseCases.GetAccessDenials;

/// <summary>
/// La otra mitad de la pantalla: lo que NO se pudo hacer.
///
/// Es la que contesta la pregunta que ninguna bitacora de acciones puede contestar: quien lo
/// intento y no pudo. Y con el filtro de "se habria bloqueado" es tambien la lista de trabajo
/// para encender el enforcement sin tumbarle el trabajo a nadie.
/// </summary>
internal sealed class GetAccessDenialsHandler(IAuditReadRepository repository)
    : IRequestHandler<GetAccessDenialsRequest, GetAccessDenialsResponse>
{
    public async Task<GetAccessDenialsResponse> Handle(
        GetAccessDenialsRequest request, CancellationToken cancellationToken)
    {
        if (request.FromUtc is { } from && request.ToUtc is { } to && from > to)
            return new GetAccessDenialsValidationFailure("El rango de fechas esta invertido.");

        var pageNumber = Paging.NormalizePageNumber(request.PageNumber);
        var pageSize = Paging.NormalizePageSize(request.PageSize);

        var filter = new AccessDenialFilter(
            request.FromUtc,
            request.ToUtc,
            Clean(request.PermissionCode),
            Clean(request.AttemptedBy),
            request.OnlyWouldHaveBeenBlocked);

        var page = await repository
            .GetDenialsPageAsync(filter, pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items
            .Select(item => new AccessDenialItemDto(
                item.PublicId,
                item.PermissionCode,
                item.Route,
                item.HttpMethod,
                item.AttemptedBy,
                item.AttemptedByDisplay,
                item.EnforcementEnabled,
                item.Blocked,
                item.OccurredAtUtc))
            .ToList();

        return new GetAccessDenialsSuccess(
            new AccessDenialPageDto(items, page.TotalCount, page.PageNumber, page.PageSize));
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
