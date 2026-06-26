using Common.Results;

namespace Ventas.Application.UseCases.RegistrarVenta;

public sealed record RegistrarVentaSuccess(
    Guid VentaId,
    decimal Total,
    int StockRestante,
    string Vendedor,
    string PaisCliente
) : RegistrarVentaResponse, ISuccess;
