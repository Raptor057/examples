using Common.Results;

namespace Orders.Application.UseCases.OrdersTree.CreateOrder.Responses;

public sealed record CreateOrderValidationFailure(string Message)
    : CreateOrderResponse, IValidationFailure;
