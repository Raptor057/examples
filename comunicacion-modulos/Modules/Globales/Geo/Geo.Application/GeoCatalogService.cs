using Geo.Contracts;
using Geo.Domain.Repositories;

namespace Geo.Application;

// Implementacion del contrato cross-module del global Geo.
public sealed class GeoCatalogService : IGeoCatalog
{
    private readonly IPaisRepository _paises;

    public GeoCatalogService(IPaisRepository paises) => _paises = paises;

    public async Task<IReadOnlyList<PaisInfo>> ListarPaisesAsync(CancellationToken ct = default)
    {
        var paises = await _paises.ListAllAsync(ct);
        return paises.Select(p => new PaisInfo(p.Codigo, p.Nombre)).ToList();
    }

    public Task<bool> ExistePaisAsync(string codigo, CancellationToken ct = default)
        => _paises.ExistsAsync(codigo, ct);
}
