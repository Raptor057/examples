using Common.Results;
using Orders.Application.Dtos;

namespace Orders.Application.UseCases.OrdersTree.CreateOrder.Responses;

public sealed record CreateOrderSuccess(CreatedOrderDto Data)
    : CreateOrderResponse, ISuccess<CreatedOrderDto>;
