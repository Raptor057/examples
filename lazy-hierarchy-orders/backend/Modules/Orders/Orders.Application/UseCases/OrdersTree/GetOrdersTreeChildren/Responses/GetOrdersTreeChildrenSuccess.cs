using Common.Results;
using Orders.Application.Dtos;

namespace Orders.Application.UseCases.OrdersTree.GetOrdersTreeChildren.Responses;

public sealed record GetOrdersTreeChildrenSuccess(OrdersTreeChildrenDto Data)
    : GetOrdersTreeChildrenResponse, ISuccess<OrdersTreeChildrenDto>;
