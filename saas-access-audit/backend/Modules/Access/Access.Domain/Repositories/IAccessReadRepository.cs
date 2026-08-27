using Access.Domain.Models;
using SaasAccessAudit.Kernel;

namespace Access.Domain.Repositories;

/// <summary>
/// Lecturas del modulo. Van por EF Core: el aislamiento por tenant lo aplica el global query
/// filter y en este archivo no hay -ni debe haber- un solo Where por tenant.
/// </summary>
public interface IAccessReadRepository
{
    /// <summary>
    /// Permisos efectivos de una persona, calculados EN CADA PETICION a partir de sus
    /// pertenencias. No se cachean entre peticiones a proposito: quitarle un rol a alguien tiene
    /// que surtir efecto en la siguiente llamada, no cuando caduque su sesion.
    /// </summary>
    Task<IReadOnlyList<string>> GetEffectivePermissionCodesAsync(
        IReadOnlyList<string> groupNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Codigo del tenant en curso, para que la pantalla lo muestre. Sale del contexto de la
    /// peticion, nunca de un parametro: el codigo es de presentacion, pero el tenant que decide
    /// que se lee ya lo fijo el token.
    /// </summary>
    Task<string> GetTenantCodeAsync(CancellationToken cancellationToken = default);

    /// <summary>Roles del tenant con las concesiones vigentes de cada uno.</summary>
    Task<IReadOnlyList<RoleWithGrants>> GetRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>Catalogo de permisos vigente, tal como quedo sembrado desde codigo.</summary>
    Task<IReadOnlyList<PermissionRow>> GetPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pagina de usuarios. Filtrar, ordenar y paginar son la misma operacion y se resuelven
    /// aqui, en el servidor: paginar en el servidor y filtrar en el cliente devuelve resultados
    /// incorrectos, no lentos.
    /// </summary>
    Task<PagedResult<UserRow>> GetUsersPageAsync(
        string? search, bool onlyActive, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
