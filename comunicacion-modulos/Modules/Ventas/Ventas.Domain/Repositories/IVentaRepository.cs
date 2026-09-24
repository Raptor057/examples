using Ventas.Domain.Entities;

namespace Ventas.Domain.Repositories;

public interface IVentaRepository
{
    Task AddAsync(Venta venta, CancellationToken ct = default);
    Task<IReadOnlyList<Venta>> ListAllAsync(CancellationToken ct = default);
}
