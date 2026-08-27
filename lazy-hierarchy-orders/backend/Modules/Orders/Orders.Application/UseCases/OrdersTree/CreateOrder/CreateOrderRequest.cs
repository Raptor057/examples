using Common.Messaging;
using Orders.Application.UseCases.OrdersTree.CreateOrder.Responses;

namespace Orders.Application.UseCases.OrdersTree.CreateOrder;

public sealed record CreateOrderLineRequest(Guid ProductPublicId, int Quantity);

/// <summary>
/// Alta de un pedido: el lado de ESCRITURA, que va por EF Core. Tampoco lleva tenant: lo sella
/// el interceptor desde el contexto autenticado al insertar.
/// </summary>
public sealed record CreateOrderRequest(
    string? OrderNumber,
    string? CustomerName,
    IReadOnlyList<CreateOrderLineRequest>? Lines)
    : IRequest<CreateOrderResponse>;
