using Access.Domain.Entities;

namespace Access.Domain.Repositories;

/// <summary>
/// Escrituras del modulo, por EF Core. El interceptor sella el tenant al insertar y mantiene las
/// fechas UTC, asi que ningun handler menciona el tenant en una sola linea.
/// </summary>
public interface IAccessWriteRepository
{
    Task<AppUser?> FindUserAsync(Guid publicId, CancellationToken cancellationToken = default);

    Task<Role?> FindRoleAsync(Guid publicId, CancellationToken cancellationToken = default);

    Task<Permission?> FindPermissionAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Desactiva al usuario SOLO si todavia estaba activo, y devuelve si el cambio ocurrio.
    /// El filtro por el estado previo es lo que hace que dos clics no la ejecuten dos veces: sin
    /// el, el segundo clic vuelve a escribir la fila y vuelve a dejar un renglon de bitacora.
    /// </summary>
    Task<bool> DeactivateUserAsync(AppUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Concede o revoca. Devuelve si hubo cambio real: conceder algo ya concedido no es un error
    /// pero tampoco es un evento, y no tiene por que ensuciar la bitacora.
    /// </summary>
    Task<bool> SetRolePermissionAsync(
        long roleId, long permissionId, bool granted, CancellationToken cancellationToken = default);
}
