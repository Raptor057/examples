using Common.Results;

namespace Orders.Application.UseCases.OrdersTree.GetOrdersTreeChildren.Responses;

public sealed record GetOrdersTreeChildrenValidationFailure(string Message)
    : GetOrdersTreeChildrenResponse, IValidationFailure;
