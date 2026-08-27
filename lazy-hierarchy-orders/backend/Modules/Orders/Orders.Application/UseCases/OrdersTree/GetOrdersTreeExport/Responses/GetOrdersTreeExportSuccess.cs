using Common.Results;
using Orders.Application.Dtos;

namespace Orders.Application.UseCases.OrdersTree.GetOrdersTreeExport.Responses;

public sealed record GetOrdersTreeExportSuccess(OrdersExportPageDto Data)
    : GetOrdersTreeExportResponse, ISuccess<OrdersExportPageDto>;
