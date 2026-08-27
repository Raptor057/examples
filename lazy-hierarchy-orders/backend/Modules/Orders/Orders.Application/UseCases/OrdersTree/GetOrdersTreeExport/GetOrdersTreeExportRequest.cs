using Common.Messaging;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeExport.Responses;

namespace Orders.Application.UseCases.OrdersTree.GetOrdersTreeExport;

/// <summary>
/// Exportacion acotada a un nodo. Toma las MISMAS coordenadas que sirven para expandirlo:
/// cero parametros nuevos, y el usuario ya sabe que va a bajar porque es lo que esta viendo.
/// </summary>
public sealed record GetOrdersTreeExportRequest(
    string? SearchType,
    string? SearchValue,
    int? Year,
    int? Month,
    long? CategoryId,
    long? ProductId,
    long? OrderId,
    int? PageNumber,
    int? PageSize)
    : IRequest<GetOrdersTreeExportResponse>;
