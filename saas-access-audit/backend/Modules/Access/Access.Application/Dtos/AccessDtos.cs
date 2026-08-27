namespace Access.Application.Dtos;

/// <summary>
/// Lo que la pantalla necesita saber de SI MISMA para dibujarse.
///
/// Este endpoint NO pide permiso: consultar lo propio -tu identidad, tus roles, tus permisos- lo
/// puede hacer cualquiera que este autenticado. Pedir un permiso aqui sobra y estorba: sin el, la
/// aplicacion no podria ni saber que esconder.
///
/// Y viaja el estado del flag junto con los permisos porque el frontend tiene que distinguir dos
/// situaciones que se ven igual: "no tengo el permiso" y "el enforcement esta apagado, asi que se
/// ve todo aunque no lo tenga".
/// </summary>
public sealed record MyAccessDto(
    string TenantCode,
    string Username,
    string DisplayName,
    IReadOnlyList<string> Groups,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    bool EnforcementEnabled);

public sealed record UserListItemDto(
    Guid PublicId,
    string Username,
    string DisplayName,
    string Email,
    bool IsActive,
    DateTime? DeactivatedAtUtc,
    IReadOnlyList<string> Groups,
    IReadOnlyList<string> Roles);

public sealed record UsersPageDto(
    IReadOnlyList<UserListItemDto> Items,
    long TotalCount,
    int PageNumber,
    int PageSize);

public sealed record PermissionDto(
    string Code,
    string Module,
    string Resource,
    string Action,
    string DisplayName,
    bool IsDestructive);

public sealed record RoleDto(
    Guid PublicId,
    string Code,
    string Name,
    IReadOnlyList<string> Groups,
    IReadOnlyList<string> Permissions);

/// <summary>
/// La matriz de la pantalla de roles: las columnas son el CATALOGO (lo que se puede conceder) y
/// las filas los roles. Van juntos en una respuesta porque separarlos permitiria dibujar una
/// matriz con columnas de una version y filas de otra.
/// </summary>
public sealed record RolesPageDto(
    IReadOnlyList<RoleDto> Roles,
    IReadOnlyList<PermissionDto> Permissions);

/// <summary>
/// Resultado de desactivar. Trae el estado del efecto externo porque la pantalla tiene que poder
/// mostrarlo como ADVERTENCIA: pintar de verde una accion cuyo segundo paso quedo pendiente es
/// mentir, y quien lea la pantalla se ira creyendo que el usuario ya no puede entrar.
/// </summary>
public sealed record DeactivateUserResultDto(
    Guid UserPublicId,
    string Username,
    bool ExternalEffectApplied,
    string? ExternalEffectError);

public sealed record SetRolePermissionResultDto(
    Guid RolePublicId,
    string PermissionCode,
    bool Granted,
    bool Changed);
