using Common.Results;

namespace Orders.Application.UseCases.OrdersTree.CreateOrder.Responses;

public sealed record CreateOrderNotFoundFailure(string Message)
    : CreateOrderResponse, INotFoundFailure;
