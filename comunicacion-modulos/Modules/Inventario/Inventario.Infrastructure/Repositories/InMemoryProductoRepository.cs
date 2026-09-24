using System.Collections.Concurrent;
using Inventario.Domain.Entities;
using Inventario.Domain.Repositories;

namespace Inventario.Infrastructure.Repositories;

// Repo en memoria con seed. Singleton en DI para que el stock persista entre
// requests durante la demo. En ArccNova esto seria un repositorio EF Core.
public sealed class InMemoryProductoRepository : IProductoRepository
{
    private readonly ConcurrentDictionary<long, Producto> _store = new();

    public InMemoryProductoRepository()
    {
        Seed(new Producto(1, "Lote residencial 200m2", 350000m, 5));
        Seed(new Producto(2, "Lote comercial 400m2", 720000m, 2));
        Seed(new Producto(3, "Saco de cemento 50kg", 210m, 100));
    }

    private void Seed(Producto p) => _store[p.Id] = p;

    public Task<IReadOnlyList<Producto>> ListAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Producto>>(_store.Values.OrderBy(p => p.Id).ToList());

    public Task<Producto?> FindByIdAsync(long id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var p) ? p : null);

    public Task SaveAsync(Producto producto, CancellationToken ct = default)
    {
        _store[producto.Id] = producto;
        return Task.CompletedTask;
    }
}
