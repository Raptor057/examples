using Audit.Application.Dtos;
using Audit.Application.UseCases.GetActionAudit.Responses;
using Audit.Domain.Models;
using Audit.Domain.Repositories;
using Common.Messaging;
using SaasAccessAudit.Kernel;

namespace Audit.Application.UseCases.GetActionAudit;

/// <summary>
/// La pantalla de consulta de la bitacora.
///
/// Es de SOLO LECTURA: no hay caso de uso que edite ni borre un renglon, ni lo va a haber. Una
/// bitacora que se puede editar no es una bitacora.
///
/// El handler normaliza lo que llega del cliente antes de que toque la base: tope al tamano de
/// pagina, recorte de los textos, y rango de fechas coherente. Ese ultimo detalle -si el desde es
/// posterior al hasta, se rechaza- evita la pantalla vacia que nadie sabe explicar.
/// </summary>
internal sealed class GetActionAuditHandler(IAuditReadRepository repository)
    : IRequestHandler<GetActionAuditRequest, GetActionAuditResponse>
{
    public async Task<GetActionAuditResponse> Handle(
        GetActionAuditRequest request, CancellationToken cancellationToken)
    {
        if (request.FromUtc is { } from && request.ToUtc is { } to && from > to)
            return new GetActionAuditValidationFailure("El rango de fechas esta invertido.");

        var pageNumber = Paging.NormalizePageNumber(request.PageNumber);
        var pageSize = Paging.NormalizePageSize(request.PageSize);

        var filter = new ActionAuditFilter(
            request.FromUtc,
            request.ToUtc,
            Clean(request.ActionCode),
            Clean(request.SubjectKey),
            Clean(request.PerformedBy),
            request.OnlyPendingExternalEffect);

        var page = await repository
            .GetActionsPageAsync(filter, pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items
            .Select(item => new ActionAuditItemDto(
                item.PublicId,
                item.ActionCode,
                item.SubjectType,
                item.SubjectKey,
                item.SubjectLabel,
                item.Reason,
                item.PerformedBy,
                item.PerformedByDisplay,
                item.AuthorizedBy,
                item.OccurredAtUtc,
                item.ExternalEffectName,
                item.ExternalEffectApplied,
                item.ExternalEffectError))
            .ToList();

        return new GetActionAuditSuccess(
            new ActionAuditPageDto(items, page.TotalCount, page.PageNumber, page.PageSize));
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
