using Common.Messaging;

namespace Ventas.Application.UseCases.RegistrarVenta;

public sealed record RegistrarVentaRequest(long ProductoId, int Cantidad, string PaisCliente)
    : IRequest<RegistrarVentaResponse>;
