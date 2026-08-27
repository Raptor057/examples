using Orders.Domain.Trees;

namespace Orders.Domain.Repositories;

/// <summary>
/// Lecturas del arbol. Fijate en lo que NO aparece en ninguna firma: el tenant. El repositorio
/// no lo recibe del handler porque no puede venir del cliente; lo resuelve el TenantScope desde
/// el contexto de la peticion, igual que haria el global query filter de EF.
/// </summary>
public interface IOrdersTreeReadRepository
{
    Task<OrdersTreeChildren> GetChildrenAsync(
        string level,
        OrdersTreeSearch search,
        OrdersTreeCoordinates coordinates,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetExportCountAsync(
        OrdersTreeSearch search,
        OrdersTreeCoordinates coordinates,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IReadOnlyList<object?>>> GetExportPageAsync(
        OrdersTreeSearch search,
        OrdersTreeCoordinates coordinates,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
