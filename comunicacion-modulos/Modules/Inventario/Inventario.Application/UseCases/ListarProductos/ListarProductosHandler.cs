using Common.Abstractions;
using Inventario.Contracts;
using Inventario.Domain.Repositories;

namespace Inventario.Application.UseCases.ListarProductos;

// Use case intra-modulo via mediator (IInteractor).
public sealed class ListarProductosHandler : IInteractor<ListarProductosRequest, ListarProductosResponse>
{
    private readonly IProductoRepository _productos;

    public ListarProductosHandler(IProductoRepository productos) => _productos = productos;

    public async Task<ListarProductosResponse> Handle(ListarProductosRequest request, CancellationToken cancellationToken)
    {
        var productos = await _productos.ListAllAsync(cancellationToken);
        var dto = productos
            .Select(p => new ProductoInfo(p.Id, p.Nombre, p.Precio, p.Stock))
            .ToList();

        return new ListarProductosSuccess(dto);
    }
}
