using System.Collections.Concurrent;
using Ventas.Domain.Entities;
using Ventas.Domain.Repositories;

namespace Ventas.Infrastructure.Repositories;

public sealed class InMemoryVentaRepository : IVentaRepository
{
    private readonly ConcurrentQueue<Venta> _ventas = new();

    public Task AddAsync(Venta venta, CancellationToken ct = default)
    {
        _ventas.Enqueue(venta);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Venta>> ListAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Venta>>(_ventas.ToList());
}
