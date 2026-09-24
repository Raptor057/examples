namespace Inventario.Contracts;

// POCOs de transporte: lo que cruza la frontera del modulo. No son entidades de
// dominio; son una vista plana y estable pensada para otros modulos.
public sealed record ProductoInfo(long ProductoId, string Nombre, decimal Precio, int StockDisponible);

public sealed record ResultadoAjusteStock(bool Exitoso, string? Error, int StockRestante);

// Contrato publico del modulo Inventario. Cualquier otro modulo que necesite datos
// de inventario depende SOLO de esta interfaz, nunca de la implementacion.
public interface IConsultaInventario
{
    Task<IReadOnlyList<ProductoInfo>> ListarAsync(CancellationToken ct = default);

    Task<ProductoInfo?> ObtenerProductoAsync(long productoId, CancellationToken ct = default);

    Task<ResultadoAjusteStock> DescontarStockAsync(long productoId, int cantidad, CancellationToken ct = default);
}
