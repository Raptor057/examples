using Common.Results;

namespace Orders.Application.UseCases.OrdersTree.CreateOrder.Responses;

public sealed record CreateOrderConflictFailure(string Message)
    : CreateOrderResponse, IConflictFailure;
