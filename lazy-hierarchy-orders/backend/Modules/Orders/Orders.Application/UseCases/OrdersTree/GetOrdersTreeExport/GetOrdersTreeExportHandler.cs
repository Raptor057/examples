using Common.Messaging;
using Orders.Application.Dtos;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeExport.Responses;
using Orders.Domain.Repositories;
using Orders.Domain.Trees;

namespace Orders.Application.UseCases.OrdersTree.GetOrdersTreeExport;

internal sealed class GetOrdersTreeExportHandler(IOrdersTreeReadRepository repository)
    : IRequestHandler<GetOrdersTreeExportRequest, GetOrdersTreeExportResponse>
{
    private const int DefaultPageSize = 5_000;
    private const int MaxPageSize = 20_000;

    private static readonly HashSet<string> AllowedSearchTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "all", "status", "customer", "ordernumber"
    };

    /// <summary>
    /// Columnas de la matriz: llave + tipo. El encabezado traducido y el formato de fecha y
    /// moneda los pone el cliente segun su idioma, para que el archivo salga con los mismos
    /// formatos que el usuario ve en pantalla.
    /// </summary>
    private static readonly OrdersExportColumnDto[] Columns =
    [
        new("orderNumber", "text"),
        new("placedAtUtc", "dateTimeUtc"),
        new("customerName", "text"),
        new("status", "text"),
        new("categoryName", "text"),
        new("sku", "text"),
        new("productName", "text"),
        new("quantity", "integer"),
        new("unitPrice", "money"),
        new("lineTotal", "money")
    ];

    public async Task<GetOrdersTreeExportResponse> Handle(
        GetOrdersTreeExportRequest request, CancellationToken cancellationToken)
    {
        var searchType = (request.SearchType ?? "all").Trim().ToLowerInvariant();
        var searchValue = (request.SearchValue ?? string.Empty).Trim();

        if (!AllowedSearchTypes.Contains(searchType))
            return new GetOrdersTreeExportValidationFailure("El tipo de busqueda no es valido.");

        if (searchType != "all" && string.IsNullOrWhiteSpace(searchValue))
            return new GetOrdersTreeExportValidationFailure("Debes capturar un valor de busqueda.");

        if (request.Month is not null && request.Year is null)
            return new GetOrdersTreeExportValidationFailure("Falta el anio.");

        var pageNumber = request.PageNumber is > 0 ? request.PageNumber.Value : 1;
        var pageSize = request.PageSize is > 0 ? Math.Min(request.PageSize.Value, MaxPageSize) : DefaultPageSize;

        var search = new OrdersTreeSearch(searchType, searchValue);
        var coordinates = new OrdersTreeCoordinates(
            request.Year, request.Month, request.CategoryId, request.ProductId, request.OrderId);

        // El total se cuenta SOLO en la primera pagina: alimenta la barra de progreso del
        // cliente. Contarlo en cada pagina duplicaria el trabajo del motor durante toda la
        // descarga, que es justo cuando menos sobra.
        var totalCount = pageNumber <= 1
            ? await repository.GetExportCountAsync(search, coordinates, cancellationToken).ConfigureAwait(false)
            : 0;

        var items = await repository.GetExportPageAsync(search, coordinates, pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);

        return new GetOrdersTreeExportSuccess(
            new OrdersExportPageDto(Columns, items, totalCount, pageNumber, pageSize));
    }
}
