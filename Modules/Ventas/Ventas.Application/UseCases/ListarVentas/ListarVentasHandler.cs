using Common.Abstractions;
using Ventas.Domain.Repositories;

namespace Ventas.Application.UseCases.ListarVentas;

public sealed class ListarVentasHandler : IInteractor<ListarVentasRequest, ListarVentasResponse>
{
    private readonly IVentaRepository _ventas;

    public ListarVentasHandler(IVentaRepository ventas) => _ventas = ventas;

    public async Task<ListarVentasResponse> Handle(ListarVentasRequest request, CancellationToken cancellationToken)
    {
        var ventas = await _ventas.ListAllAsync(cancellationToken);
        var dto = ventas
            .Select(v => new VentaResumen(v.Id, v.ProductoId, v.Cantidad, v.Total, v.Vendedor, v.PaisCliente, v.FechaUtc))
            .ToList();

        return new ListarVentasSuccess(dto);
    }
}
