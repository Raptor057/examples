using Access.Application.UseCases.SetRolePermission.Responses;
using Common.Messaging;

namespace Access.Application.UseCases.SetRolePermission;

/// <summary>
/// Conceder o revocar un permiso a un rol.
///
/// No borra un solo dato y aun asi es una accion sensible: cambia QUE PUEDEN HACER OTRAS
/// PERSONAS. Por eso lleva permiso propio, motivo obligatorio y bitacora, igual que la que
/// desactiva a alguien.
/// </summary>
public sealed record SetRolePermissionRequest(
    Guid RolePublicId,
    string? PermissionCode,
    bool Granted,
    string? Reason) : IRequest<SetRolePermissionResponse>;
