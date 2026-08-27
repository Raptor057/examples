using Orders.Domain.Repositories;
using Orders.Domain.Trees;

namespace Orders.Tests;

/// <summary>
/// Doble de prueba escrito a mano: el ejemplo no arrastra una libreria de mocks para tres
/// pruebas. Registra con que argumentos lo llamaron, que es lo unico que las pruebas necesitan.
/// </summary>
internal sealed class FakeOrdersTreeReadRepository : IOrdersTreeReadRepository
{
    public string? LastLevel { get; private set; }

    public OrdersTreeSearch? LastSearch { get; private set; }

    public OrdersTreeCoordinates? LastCoordinates { get; private set; }

    public int LastPageNumber { get; private set; }

    public int LastPageSize { get; private set; }

    public bool WasCalled { get; private set; }

    public IReadOnlyList<OrdersTreeNode> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public Task<OrdersTreeChildren> GetChildrenAsync(
        string level,
        OrdersTreeSearch search,
        OrdersTreeCoordinates coordinates,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        LastLevel = level;
        LastSearch = search;
        LastCoordinates = coordinates;
        LastPageNumber = pageNumber;
        LastPageSize = pageSize;

        return Task.FromResult(new OrdersTreeChildren(
            level, Items, TotalCount, pageNumber, pageSize, HasMore: false));
    }

    public Task<int> GetExportCountAsync(
        OrdersTreeSearch search, OrdersTreeCoordinates coordinates, CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        LastSearch = search;
        LastCoordinates = coordinates;
        return Task.FromResult(TotalCount);
    }

    public Task<IReadOnlyList<IReadOnlyList<object?>>> GetExportPageAsync(
        OrdersTreeSearch search,
        OrdersTreeCoordinates coordinates,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        LastSearch = search;
        LastCoordinates = coordinates;
        LastPageNumber = pageNumber;
        LastPageSize = pageSize;
        return Task.FromResult<IReadOnlyList<IReadOnlyList<object?>>>([]);
    }
}
