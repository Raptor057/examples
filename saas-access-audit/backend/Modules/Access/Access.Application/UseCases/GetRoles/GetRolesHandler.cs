using Access.Application.Dtos;
using Access.Application.UseCases.GetRoles.Responses;
using Access.Domain.Repositories;
using Common.Messaging;

namespace Access.Application.UseCases.GetRoles;

/// <summary>
/// La matriz de roles y permisos.
///
/// Las columnas salen de la tabla de permisos, que a su vez la escribe el sembrador desde el
/// catalogo en codigo. Ese rodeo es el que hace imposible conceder un permiso que el codigo no
/// conoce: la pantalla solo puede ofrecer lo que el catalogo sembro.
/// </summary>
internal sealed class GetRolesHandler(IAccessReadRepository repository)
    : IRequestHandler<GetRolesRequest, GetRolesResponse>
{
    public async Task<GetRolesResponse> Handle(GetRolesRequest request, CancellationToken cancellationToken)
    {
        var roles = await repository.GetRolesAsync(cancellationToken).ConfigureAwait(false);
        var permissions = await repository.GetPermissionsAsync(cancellationToken).ConfigureAwait(false);

        return new GetRolesSuccess(new RolesPageDto(
            roles
                .Select(role => new RoleDto(role.PublicId, role.Code, role.Name, role.GroupNames, role.PermissionCodes))
                .ToList(),
            permissions
                .Select(permission => new PermissionDto(
                    permission.Code,
                    permission.Module,
                    permission.Resource,
                    permission.Action,
                    permission.DisplayName,
                    permission.IsDestructive))
                .ToList()));
    }
}
