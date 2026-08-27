using Common.Messaging;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeChildren.Responses;

namespace Orders.Application.UseCases.OrdersTree.GetOrdersTreeChildren;

/// <summary>
/// Entrada del unico endpoint de niveles.
///
/// Mira lo que NO esta aqui: el tenant. Si existiera como parametro, cualquiera podria leer el
/// arbol de otra empresa cambiando un numero en la URL. Sale del token y de ningun otro lado.
/// Hay una prueba que falla si alguien lo agrega.
/// </summary>
public sealed record GetOrdersTreeChildrenRequest(
    string? Level,
    string? SearchType,
    string? SearchValue,
    int? Year,
    int? Month,
    long? CategoryId,
    long? ProductId,
    long? OrderId,
    int? PageNumber,
    int? PageSize)
    : IRequest<GetOrdersTreeChildrenResponse>;
