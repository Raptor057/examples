using Geo.Domain.Entities;
using Geo.Domain.Repositories;

namespace Geo.Infrastructure.Repositories;

// Seed MX + US + CA, como el catalogo geo de ArccNova (seed MX/US).
public sealed class InMemoryPaisRepository : IPaisRepository
{
    private static readonly IReadOnlyList<Pais> Paises = new[]
    {
        new Pais("MX", "Mexico"),
        new Pais("US", "Estados Unidos"),
        new Pais("CA", "Canada"),
    };

    public Task<IReadOnlyList<Pais>> ListAllAsync(CancellationToken ct = default)
        => Task.FromResult(Paises);

    public Task<bool> ExistsAsync(string codigo, CancellationToken ct = default)
        => Task.FromResult(Paises.Any(p => string.Equals(p.Codigo, codigo, StringComparison.OrdinalIgnoreCase)));
}
