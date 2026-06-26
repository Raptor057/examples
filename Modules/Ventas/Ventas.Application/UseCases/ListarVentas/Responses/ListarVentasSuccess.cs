using Common.Results;

namespace Ventas.Application.UseCases.ListarVentas;

public sealed record VentaResumen(
    Guid Id,
    long ProductoId,
    int Cantidad,
    decimal Total,
    string Vendedor,
    string PaisCliente,
    DateTime FechaUtc);

public sealed record ListarVentasSuccess(IReadOnlyList<VentaResumen> Ventas)
    : ListarVentasResponse, ISuccess;
