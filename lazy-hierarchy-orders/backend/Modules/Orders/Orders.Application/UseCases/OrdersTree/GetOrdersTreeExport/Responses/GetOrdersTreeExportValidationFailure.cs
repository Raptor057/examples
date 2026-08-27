using Common.Results;

namespace Orders.Application.UseCases.OrdersTree.GetOrdersTreeExport.Responses;

public sealed record GetOrdersTreeExportValidationFailure(string Message)
    : GetOrdersTreeExportResponse, IValidationFailure;
