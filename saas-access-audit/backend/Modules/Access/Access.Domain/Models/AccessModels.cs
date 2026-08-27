namespace Access.Domain.Models;

/// <summary>Un permiso del catalogo, tal como quedo sembrado desde codigo.</summary>
public sealed record PermissionRow(
    string Code,
    string Module,
    string Resource,
    string Action,
    string DisplayName,
    bool IsDestructive);

/// <summary>Un rol con los codigos de permiso que tiene concedidos ahora mismo.</summary>
public sealed record RoleWithGrants(
    Guid PublicId,
    string Code,
    string Name,
    IReadOnlyList<string> GroupNames,
    IReadOnlyList<string> PermissionCodes);

/// <summary>
/// Un usuario de la consola. Trae los roles a los que llega por sus pertenencias, que es lo que
/// permite entender de un vistazo por que alguien pudo hacer algo y otro no.
/// </summary>
public sealed record UserRow(
    Guid PublicId,
    string Username,
    string DisplayName,
    string Email,
    bool IsActive,
    DateTime? DeactivatedAtUtc,
    IReadOnlyList<string> GroupNames,
    IReadOnlyList<string> RoleNames);
