using Orders.Domain.Entities;

namespace Orders.Domain.Repositories;

/// <summary>
/// Escrituras. Van por EF Core: el tenant lo pone el global query filter y el chasis, no una
/// columna que el handler tenga que acordarse de llenar.
/// </summary>
public interface IOrderWriteRepository
{
    Task<Product?> GetProductAsync(Guid productPublicId, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(CustomerOrder order, CancellationToken cancellationToken = default);

    Task<bool> OrderNumberExistsAsync(string orderNumber, CancellationToken cancellationToken = default);
}
