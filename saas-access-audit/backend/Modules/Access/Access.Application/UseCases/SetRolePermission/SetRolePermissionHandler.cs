using Access.Application.Dtos;
using Access.Application.UseCases.SetRolePermission.Responses;
using Access.Contracts;
using Access.Domain.Repositories;
using Audit.Contracts;
using Common.Messaging;

namespace Access.Application.UseCases.SetRolePermission;

/// <summary>
/// Conceder o revocar un permiso a un rol: la segunda accion sensible del ejemplo.
///
/// Aqui esta la defensa que hace que el catalogo en codigo sea de verdad la unica fuente:
/// antes de tocar la base se comprueba que el codigo pedido EXISTE en
/// <see cref="PermissionCatalog"/>. Sin esa linea, un cliente podria conceder un permiso
/// inventado, la fila quedaria en la tabla, la pantalla lo mostraria concedido, y ningun endpoint
/// lo exigiria jamas: un permiso fantasma que parece dar acceso y no da nada.
///
/// No borra un solo dato y aun asi deja bitacora, porque cambia lo que OTRAS PERSONAS pueden
/// hacer. La regla lista explicitamente los cambios de permisos y roles entre lo que se audita.
/// </summary>
internal sealed class SetRolePermissionHandler(
    IAccessWriteRepository repository,
    IActionAuditLog auditLog)
    : IRequestHandler<SetRolePermissionRequest, SetRolePermissionResponse>
{
    public async Task<SetRolePermissionResponse> Handle(
        SetRolePermissionRequest request, CancellationToken cancellationToken)
    {
        var (reason, reasonError) = ReasonPolicy.Normalize(request.Reason);
        if (reason is null)
            return new SetRolePermissionValidationFailure(reasonError!);

        var permissionCode = (request.PermissionCode ?? string.Empty).Trim();

        // El catalogo manda. Un codigo que no esta aqui no existe, aunque quede una fila vieja
        // en la tabla de permisos.
        if (!PermissionCatalog.Contains(permissionCode))
            return new SetRolePermissionValidationFailure(
                $"El permiso {permissionCode} no esta en el catalogo del producto.");

        var role = await repository.FindRoleAsync(request.RolePublicId, cancellationToken).ConfigureAwait(false);
        if (role is null)
            return new SetRolePermissionNotFoundFailure("No se encontro ese rol.");

        var permission = await repository.FindPermissionAsync(permissionCode, cancellationToken).ConfigureAwait(false);
        if (permission is null)
            return new SetRolePermissionNotFoundFailure(
                $"El permiso {permissionCode} esta en el catalogo pero todavia no se sembro. Reinicia el backend.");

        var changed = await repository
            .SetRolePermissionAsync(role.Id, permission.Id, request.Granted, cancellationToken)
            .ConfigureAwait(false);

        // Solo se audita el cambio REAL. Conceder algo ya concedido no es un error, pero tampoco
        // es un evento: registrarlo llenaria la bitacora de renglones que no pasaron.
        if (changed)
        {
            var verb = request.Granted ? "concedido" : "revocado";
            await auditLog.RecordAsync(
                new ActionAuditRecord(
                    ActionCode: PermissionCatalog.RolesManage,
                    SubjectType: "role-permission",
                    // Llave compuesta, pero llave al fin: identifica la concesion exacta y no
                    // depende de como se llame el rol hoy.
                    SubjectKey: $"{role.PublicId}:{permission.Code}",
                    SubjectLabel: $"{role.Name} / {permission.DisplayName} ({verb})",
                    Reason: reason),
                cancellationToken).ConfigureAwait(false);
        }

        return new SetRolePermissionSuccess(new SetRolePermissionResultDto(
            role.PublicId, permission.Code, request.Granted, changed));
    }
}
