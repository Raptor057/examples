using Inventario.Domain.Entities;

namespace Inventario.Domain.Repositories;

// Interfaz de repositorio (puerto). La implementacion concreta vive en Infrastructure.
public interface IProductoRepository
{
    Task<IReadOnlyList<Producto>> ListAllAsync(CancellationToken ct = default);
    Task<Producto?> FindByIdAsync(long id, CancellationToken ct = default);
    Task SaveAsync(Producto producto, CancellationToken ct = default);
}
