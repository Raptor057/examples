using Access.Application.Dtos;
using Access.Application.UseCases.GetUsers.Responses;
using Access.Domain.Repositories;
using Common.Messaging;
using SaasAccessAudit.Kernel;

namespace Access.Application.UseCases.GetUsers;

/// <summary>
/// La lista que administra la consola.
///
/// Filtrar, ordenar y paginar se resuelven en el servidor, y el tamano de pagina se normaliza
/// contra un tope antes de llegar a la base: un pageSize que llega del cliente sin validar
/// convierte esta pantalla en una descarga completa de la tabla de usuarios.
/// </summary>
internal sealed class GetUsersHandler(IAccessReadRepository repository)
    : IRequestHandler<GetUsersRequest, GetUsersResponse>
{
    public async Task<GetUsersResponse> Handle(GetUsersRequest request, CancellationToken cancellationToken)
    {
        var pageNumber = Paging.NormalizePageNumber(request.PageNumber);
        var pageSize = Paging.NormalizePageSize(request.PageSize);
        var search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();

        var page = await repository
            .GetUsersPageAsync(search, request.OnlyActive, pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items
            .Select(user => new UserListItemDto(
                user.PublicId,
                user.Username,
                user.DisplayName,
                user.Email,
                user.IsActive,
                user.DeactivatedAtUtc,
                user.GroupNames,
                user.RoleNames))
            .ToList();

        return new GetUsersSuccess(new UsersPageDto(items, page.TotalCount, page.PageNumber, page.PageSize));
    }
}
