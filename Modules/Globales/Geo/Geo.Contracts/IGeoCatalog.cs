namespace Geo.Contracts;

public sealed record PaisInfo(string Codigo, string Nombre);

// Contrato del catalogo geografico que consumen otros modulos (validar pais, listar, etc.).
public interface IGeoCatalog
{
    Task<IReadOnlyList<PaisInfo>> ListarPaisesAsync(CancellationToken ct = default);

    Task<bool> ExistePaisAsync(string codigo, CancellationToken ct = default);
}
