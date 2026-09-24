using Inventario.Contracts;
using Inventario.Domain.Repositories;

namespace Inventario.Application;

// Implementacion del contrato publico del modulo. Traduce entre la entidad de
// dominio (Producto) y el POCO de transporte (ProductoInfo). Es el unico punto
// por el que otros modulos "entran" a Inventario.
public sealed class ConsultaInventarioService : IConsultaInventario
{
    private readonly IProductoRepository _productos;

    public ConsultaInventarioService(IProductoRepository productos) => _productos = productos;

    public async Task<IReadOnlyList<ProductoInfo>> ListarAsync(CancellationToken ct = default)
    {
        var productos = await _productos.ListAllAsync(ct);
        return productos.Select(Map).ToList();
    }

    public async Task<ProductoInfo?> ObtenerProductoAsync(long productoId, CancellationToken ct = default)
    {
        var producto = await _productos.FindByIdAsync(productoId, ct);
        return producto is null ? null : Map(producto);
    }

    public async Task<ResultadoAjusteStock> DescontarStockAsync(long productoId, int cantidad, CancellationToken ct = default)
    {
        var producto = await _productos.FindByIdAsync(productoId, ct);
        if (producto is null)
            return new ResultadoAjusteStock(false, $"El producto {productoId} no existe.", 0);

        if (!producto.HayStockPara(cantidad))
            return new ResultadoAjusteStock(false, $"Stock insuficiente (disponible: {producto.Stock}).", producto.Stock);

        producto.Descontar(cantidad);
        await _productos.SaveAsync(producto, ct);
        return new ResultadoAjusteStock(true, null, producto.Stock);
    }

    private static ProductoInfo Map(Domain.Entities.Producto p) =>
        new(p.Id, p.Nombre, p.Precio, p.Stock);
}
