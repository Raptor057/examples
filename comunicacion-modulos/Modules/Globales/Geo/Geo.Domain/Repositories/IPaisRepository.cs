using Geo.Domain.Entities;

namespace Geo.Domain.Repositories;

public interface IPaisRepository
{
    Task<IReadOnlyList<Pais>> ListAllAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(string codigo, CancellationToken ct = default);
}
