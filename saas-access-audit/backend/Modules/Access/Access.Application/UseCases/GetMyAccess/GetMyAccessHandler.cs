using Access.Application.Dtos;
using Access.Application.UseCases.GetMyAccess.Responses;
using Access.Domain.Repositories;
using Common.Messaging;
using SaasAccessAudit.Kernel;

namespace Access.Application.UseCases.GetMyAccess;

/// <summary>
/// Lo que la aplicacion necesita para saber que esconder.
///
/// Tres decisiones visibles aqui:
///
/// 1. La identidad sale de <see cref="IUserContextAccessor"/>, es decir del token. El request no
///    la trae y no puede traerla.
/// 2. Los permisos se CALCULAN a partir de las pertenencias, en esta misma peticion. No vienen
///    del token: si vinieran, quitarle un rol a alguien no surtiria efecto hasta que caducara su
///    sesion, y "ya le quite el permiso y sigue entrando" es un reporte que nadie quiere recibir.
/// 3. Se devuelve tambien el estado del flag de enforcement, porque la pantalla no puede
///    distinguir sin el entre "no tengo permiso" y "el flag esta apagado y se ve todo".
/// </summary>
internal sealed class GetMyAccessHandler(
    IUserContextAccessor userAccessor,
    ITenantContextAccessor tenantAccessor,
    IAccessReadRepository repository,
    IAccessControlSettings settings)
    : IRequestHandler<GetMyAccessRequest, GetMyAccessResponse>
{
    public async Task<GetMyAccessResponse> Handle(GetMyAccessRequest request, CancellationToken cancellationToken)
    {
        if (userAccessor.Current is not { } user)
            return new GetMyAccessNotFoundFailure("La peticion no trae identidad.");

        var permissions = await repository
            .GetEffectivePermissionCodesAsync(user.Groups, cancellationToken)
            .ConfigureAwait(false);

        // Los roles se derivan de los mismos grupos: sirven para explicar en pantalla POR QUE
        // alguien tiene lo que tiene, que es la mitad de diagnosticar un 403.
        var roles = await repository.GetRolesAsync(cancellationToken).ConfigureAwait(false);
        var myRoles = roles
            .Where(role => role.GroupNames.Any(group => user.Groups.Contains(group, StringComparer.OrdinalIgnoreCase)))
            .Select(role => role.Name)
            .ToList();

        var tenantCode = tenantAccessor.Current is null
            ? string.Empty
            : await repository.GetTenantCodeAsync(cancellationToken).ConfigureAwait(false);

        return new GetMyAccessSuccess(new MyAccessDto(
            tenantCode,
            user.Username,
            user.DisplayName,
            user.Groups,
            myRoles,
            permissions,
            settings.EnforcePermissions));
    }
}
