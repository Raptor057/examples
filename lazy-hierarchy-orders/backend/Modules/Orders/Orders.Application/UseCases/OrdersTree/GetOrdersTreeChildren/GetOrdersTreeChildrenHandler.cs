using Common.Messaging;
using Orders.Application.Dtos;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeChildren.Responses;
using Orders.Domain.Repositories;
using Orders.Domain.Trees;

namespace Orders.Application.UseCases.OrdersTree.GetOrdersTreeChildren;

/// <summary>
/// UN solo caso de uso sirve los seis niveles del arbol. El "level" no es una tabla ni un SQL:
/// es una llave de un conjunto cerrado, y cada nivel declara que coordenadas necesita.
/// </summary>
internal sealed class GetOrdersTreeChildrenHandler(IOrdersTreeReadRepository repository)
    : IRequestHandler<GetOrdersTreeChildrenRequest, GetOrdersTreeChildrenResponse>
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    /// <summary>
    /// Lista blanca de tipos de busqueda. Duplica a proposito el switch cerrado del SQL: aqui
    /// para dar un mensaje util al usuario, alla para que un tipo inesperado devuelva vacio en
    /// lugar de la tabla entera. Las dos capas dicen que no; ninguna confia en la otra.
    /// </summary>
    private static readonly HashSet<string> AllowedSearchTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "all", "status", "customer", "ordernumber"
    };

    public async Task<GetOrdersTreeChildrenResponse> Handle(
        GetOrdersTreeChildrenRequest request, CancellationToken cancellationToken)
    {
        var level = (request.Level ?? string.Empty).Trim().ToLowerInvariant();
        var searchType = (request.SearchType ?? "all").Trim().ToLowerInvariant();
        var searchValue = (request.SearchValue ?? string.Empty).Trim();

        // Sin esta linea, "level" es una cadena del cliente que decide que consulta corre.
        if (!OrdersTreeLevels.Allowed.Contains(level))
            return new GetOrdersTreeChildrenValidationFailure("El nivel solicitado no es valido.");

        if (!AllowedSearchTypes.Contains(searchType))
            return new GetOrdersTreeChildrenValidationFailure("El tipo de busqueda no es valido.");

        if (searchType != "all" && string.IsNullOrWhiteSpace(searchValue))
            return new GetOrdersTreeChildrenValidationFailure("Debes capturar un valor de busqueda.");

        // Validacion progresiva. Verbosa a proposito: el dia que alguien agregue un nivel, el
        // compilador no le va a avisar; esta escalera si. Un nivel profundo sin sus coordenadas
        // no es una consulta vacia, es una consulta SIN FILTRO sobre millones de filas.
        if (level is OrdersTreeLevels.Month or OrdersTreeLevels.Category or OrdersTreeLevels.Product or OrdersTreeLevels.Order
            && request.Year is null)
            return new GetOrdersTreeChildrenValidationFailure("Falta el anio.");

        if (level is OrdersTreeLevels.Category or OrdersTreeLevels.Product or OrdersTreeLevels.Order
            && request.Month is null)
            return new GetOrdersTreeChildrenValidationFailure("Falta el mes.");

        if (level is OrdersTreeLevels.Product or OrdersTreeLevels.Order && request.CategoryId is null)
            return new GetOrdersTreeChildrenValidationFailure("Falta la categoria.");

        if (level is OrdersTreeLevels.Order && request.ProductId is null)
            return new GetOrdersTreeChildrenValidationFailure("Falta el producto.");

        if (level is OrdersTreeLevels.Leaf && request.OrderId is null)
            return new GetOrdersTreeChildrenValidationFailure("Falta el pedido.");

        // El tamano de pagina llega del cliente y por eso se topa. Solo el nivel de fan-out la usa.
        var pageNumber = request.PageNumber is > 0 ? request.PageNumber.Value : 1;
        var pageSize = request.PageSize is > 0 ? Math.Min(request.PageSize.Value, MaxPageSize) : DefaultPageSize;

        var children = await repository.GetChildrenAsync(
            level,
            new OrdersTreeSearch(searchType, searchValue),
            new OrdersTreeCoordinates(request.Year, request.Month, request.CategoryId, request.ProductId, request.OrderId),
            pageNumber,
            pageSize,
            cancellationToken).ConfigureAwait(false);

        return new GetOrdersTreeChildrenSuccess(new OrdersTreeChildrenDto(
            children.Level,
            children.Items.Select(MapNode).ToList(),
            children.TotalCount,
            children.PageNumber,
            children.PageSize,
            children.HasMore));
    }

    // Recursivo porque la hoja llega con su subarbol ya armado.
    private static OrdersTreeNodeDto MapNode(OrdersTreeNode node) => new(
        node.Id,
        node.NodeType,
        node.Label,
        node.NextLevel,
        node.Coordinates.Year,
        node.Coordinates.Month,
        node.Coordinates.CategoryId,
        node.Coordinates.ProductId,
        node.Coordinates.OrderId,
        new OrdersTreeMetricsDto(
            node.Metrics.OrderCount,
            node.Metrics.TotalAmount,
            node.Metrics.LineCount,
            node.Metrics.UnitCount,
            node.Metrics.UnitPrice),
        node.Details,
        node.Children.Select(MapNode).ToList());
}
