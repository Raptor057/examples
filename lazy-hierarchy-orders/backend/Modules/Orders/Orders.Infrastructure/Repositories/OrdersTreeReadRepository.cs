using System.Data;
using System.Globalization;
using Dapper;
using LazyHierarchy.Shared.Postgres;
using LazyHierarchy.Shared.Postgres.Markers;
using LazyHierarchy.Shared.Tenancy;
using Orders.Domain.Repositories;
using Orders.Domain.Trees;
using Orders.Infrastructure.Persistence.DbModels.MainDb;
using Orders.Infrastructure.Persistence.Sql.MainDb;

namespace Orders.Infrastructure.Repositories;

/// <summary>
/// El lado Dapper del ejemplo: SOLO lecturas del arbol y de la exportacion.
///
/// Por que no EF aqui: el nivel pesado es un CTE que pagina primero y un LEFT JOIN LATERAL que
/// enriquece despues, mas COUNT(*) OVER() para el total real. EF no expresa eso sin caer en
/// SQL crudo igualmente, y la consulta que generaria enriquece sobre el universo entero.
///
/// Que se paga por bajar a SQL crudo en un proyecto con tenancy: el aislamiento deja de ser
/// automatico. Se compensa con TenantScope (un solo fragmento, un solo lugar donde se pone el
/// valor) y con la prueba que recorre estas consultas. Ver el README.
/// </summary>
internal sealed class OrdersTreeReadRepository(
    ConfigurationSqlDbConnection<MainDb> db,
    TenantScope tenantScope)
    : IOrdersTreeReadRepository
{
    // Explicito y mas alto en la exportacion: una pagina de 20 mil filas tarda mas que una
    // expansion de arbol, y morir por timeout a mitad de una descarga larga es lo peor que
    // puede pasarle a este flujo.
    private const int TreeCommandTimeoutSeconds = 30;
    private const int ExportCommandTimeoutSeconds = 180;

    public async Task<OrdersTreeChildren> GetChildrenAsync(
        string level,
        OrdersTreeSearch search,
        OrdersTreeCoordinates coordinates,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return level switch
        {
            OrdersTreeLevels.Year => await YearsAsync(search, cancellationToken).ConfigureAwait(false),
            OrdersTreeLevels.Month => await MonthsAsync(search, coordinates, cancellationToken).ConfigureAwait(false),
            OrdersTreeLevels.Category => await CategoriesAsync(search, coordinates, cancellationToken).ConfigureAwait(false),
            OrdersTreeLevels.Product => await ProductsAsync(search, coordinates, cancellationToken).ConfigureAwait(false),
            OrdersTreeLevels.Order => await OrdersAsync(search, coordinates, pageNumber, pageSize, cancellationToken).ConfigureAwait(false),
            OrdersTreeLevels.Leaf => await LeafAsync(search, coordinates, cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Nivel fuera del conjunto cerrado.")
        };
    }

    // --- Niveles ---------------------------------------------------------------------------

    private async Task<OrdersTreeChildren> YearsAsync(OrdersTreeSearch search, CancellationToken cancellationToken)
    {
        var parameters = BaseParameters(search);
        var rows = await db.QueryAsync<TreeYearRow>(
            OrdersTreeSql.Years(search.SearchType), parameters,
            commandTimeout: TreeCommandTimeoutSeconds, cancellationToken: cancellationToken).ConfigureAwait(false);

        var items = rows.Select(row => new OrdersTreeNode(
            Id: $"y:{row.YearNumber}",
            NodeType: "year",
            Label: row.YearNumber.ToString(CultureInfo.InvariantCulture),
            NextLevel: OrdersTreeLevels.NextOf(OrdersTreeLevels.Year),
            Coordinates: new OrdersTreeCoordinates(Year: row.YearNumber),
            Metrics: new OrdersTreeMetrics(OrderCount: row.OrderCount, TotalAmount: row.TotalAmount),
            Details: EmptyDetails,
            Children: [])).ToList();

        return Complete(OrdersTreeLevels.Year, items);
    }

    private async Task<OrdersTreeChildren> MonthsAsync(
        OrdersTreeSearch search, OrdersTreeCoordinates coordinates, CancellationToken cancellationToken)
    {
        var year = coordinates.Year!.Value;
        var parameters = BaseParameters(search);
        AddPeriod(parameters, year, null);

        var rows = await db.QueryAsync<TreeMonthRow>(
            OrdersTreeSql.Months(search.SearchType), parameters,
            commandTimeout: TreeCommandTimeoutSeconds, cancellationToken: cancellationToken).ConfigureAwait(false);

        var items = rows.Select(row => new OrdersTreeNode(
            Id: $"m:{year}-{row.MonthNumber:00}",
            NodeType: "month",
            Label: row.MonthNumber.ToString(CultureInfo.InvariantCulture),
            NextLevel: OrdersTreeLevels.NextOf(OrdersTreeLevels.Month),
            Coordinates: new OrdersTreeCoordinates(Year: year, Month: row.MonthNumber),
            Metrics: new OrdersTreeMetrics(OrderCount: row.OrderCount, TotalAmount: row.TotalAmount),
            Details: EmptyDetails,
            Children: [])).ToList();

        return Complete(OrdersTreeLevels.Month, items);
    }

    private async Task<OrdersTreeChildren> CategoriesAsync(
        OrdersTreeSearch search, OrdersTreeCoordinates coordinates, CancellationToken cancellationToken)
    {
        var year = coordinates.Year!.Value;
        var month = coordinates.Month!.Value;
        var parameters = BaseParameters(search);
        AddPeriod(parameters, year, month);

        var rows = await db.QueryAsync<TreeCategoryRow>(
            OrdersTreeSql.Categories(search.SearchType), parameters,
            commandTimeout: TreeCommandTimeoutSeconds, cancellationToken: cancellationToken).ConfigureAwait(false);

        var items = rows.Select(row => new OrdersTreeNode(
            Id: $"c:{year}-{month:00}:{row.CategoryId}",
            NodeType: "category",
            Label: row.Name,
            NextLevel: OrdersTreeLevels.NextOf(OrdersTreeLevels.Category),
            Coordinates: new OrdersTreeCoordinates(Year: year, Month: month, CategoryId: row.CategoryId),
            Metrics: new OrdersTreeMetrics(OrderCount: row.OrderCount, TotalAmount: row.TotalAmount),
            Details: new Dictionary<string, string?> { ["code"] = row.Code },
            Children: [])).ToList();

        return Complete(OrdersTreeLevels.Category, items);
    }

    private async Task<OrdersTreeChildren> ProductsAsync(
        OrdersTreeSearch search, OrdersTreeCoordinates coordinates, CancellationToken cancellationToken)
    {
        var year = coordinates.Year!.Value;
        var month = coordinates.Month!.Value;
        var categoryId = coordinates.CategoryId!.Value;

        var parameters = BaseParameters(search);
        AddPeriod(parameters, year, month);
        parameters.Add("CategoryId", categoryId, DbType.Int64);

        var rows = await db.QueryAsync<TreeProductRow>(
            OrdersTreeSql.Products(search.SearchType), parameters,
            commandTimeout: TreeCommandTimeoutSeconds, cancellationToken: cancellationToken).ConfigureAwait(false);

        var items = rows.Select(row => new OrdersTreeNode(
            Id: $"p:{year}-{month:00}:{categoryId}:{row.ProductId}",
            NodeType: "product",
            Label: row.Name,
            NextLevel: OrdersTreeLevels.NextOf(OrdersTreeLevels.Product),
            Coordinates: new OrdersTreeCoordinates(Year: year, Month: month, CategoryId: categoryId, ProductId: row.ProductId),
            Metrics: new OrdersTreeMetrics(OrderCount: row.OrderCount, TotalAmount: row.TotalAmount),
            Details: new Dictionary<string, string?> { ["sku"] = row.Sku },
            Children: [])).ToList();

        return Complete(OrdersTreeLevels.Product, items);
    }

    /// <summary>El unico nivel paginado. El total real lo da COUNT(*) OVER() dentro del CTE.</summary>
    private async Task<OrdersTreeChildren> OrdersAsync(
        OrdersTreeSearch search, OrdersTreeCoordinates coordinates, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var year = coordinates.Year!.Value;
        var month = coordinates.Month!.Value;
        var productId = coordinates.ProductId!.Value;

        var parameters = BaseParameters(search);
        AddPeriod(parameters, year, month);
        parameters.Add("ProductId", productId, DbType.Int64);
        parameters.Add("Offset", (pageNumber - 1) * pageSize, DbType.Int32);
        parameters.Add("PageSize", pageSize, DbType.Int32);

        var rows = (await db.QueryAsync<TreeOrderRow>(
            OrdersTreeSql.OrdersPage(search.SearchType), parameters,
            commandTimeout: TreeCommandTimeoutSeconds, cancellationToken: cancellationToken).ConfigureAwait(false)).ToList();

        var items = rows.Select(row => new OrdersTreeNode(
            Id: $"o:{row.OrderId}",
            NodeType: "order",
            Label: row.OrderNumber,
            NextLevel: OrdersTreeLevels.NextOf(OrdersTreeLevels.Order),
            Coordinates: new OrdersTreeCoordinates(
                Year: year, Month: month, CategoryId: coordinates.CategoryId, ProductId: productId, OrderId: row.OrderId),
            Metrics: new OrdersTreeMetrics(
                OrderCount: 1,
                TotalAmount: row.TotalAmount,
                LineCount: row.LineCount,
                UnitCount: row.UnitCount),
            Details: new Dictionary<string, string?>
            {
                ["customerName"] = row.CustomerName,
                ["status"] = row.Status,
                ["placedAtUtc"] = row.PlacedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                ["focusUnitCount"] = row.FocusUnitCount.ToString(CultureInfo.InvariantCulture),
                ["focusAmount"] = row.FocusAmount.ToString(CultureInfo.InvariantCulture)
            },
            Children: [])).ToList();

        var totalCount = rows.Count > 0 ? rows[0].TotalCount : 0;
        return new OrdersTreeChildren(
            OrdersTreeLevels.Order, items, totalCount, pageNumber, pageSize,
            HasMore: (long)pageNumber * pageSize < totalCount);
    }

    /// <summary>
    /// La hoja devuelve su SUBARBOL completo: las lineas del pedido y su resumen, ya armados y
    /// con nextLevel null en todos. Una llamada en vez de tres, y el usuario ve el detalle de
    /// golpe. Son dos sentencias contra la base, pero un solo viaje del cliente.
    /// </summary>
    private async Task<OrdersTreeChildren> LeafAsync(
        OrdersTreeSearch search, OrdersTreeCoordinates coordinates, CancellationToken cancellationToken)
    {
        var orderId = coordinates.OrderId!.Value;
        var parameters = BaseParameters(search);
        parameters.Add("OrderId", orderId, DbType.Int64);

        var header = (await db.QueryAsync<TreeOrderHeaderRow>(
            OrdersTreeSql.LeafHeader(search.SearchType), parameters,
            commandTimeout: TreeCommandTimeoutSeconds, cancellationToken: cancellationToken).ConfigureAwait(false)).FirstOrDefault();

        if (header is null)
            return Complete(OrdersTreeLevels.Leaf, []);

        var lineRows = (await db.QueryAsync<TreeOrderLineRow>(
            OrdersTreeSql.LeafLines(search.SearchType), parameters,
            commandTimeout: TreeCommandTimeoutSeconds, cancellationToken: cancellationToken).ConfigureAwait(false)).ToList();

        var lineNodes = lineRows.Select(row => new OrdersTreeNode(
            Id: $"ol:{row.LineId}",
            NodeType: "orderLine",
            Label: row.ProductName,
            NextLevel: null,
            Coordinates: new OrdersTreeCoordinates(
                Year: coordinates.Year, Month: coordinates.Month,
                CategoryId: coordinates.CategoryId, ProductId: coordinates.ProductId, OrderId: orderId),
            Metrics: new OrdersTreeMetrics(
                TotalAmount: row.LineTotal, UnitCount: row.Quantity, UnitPrice: row.UnitPrice),
            Details: new Dictionary<string, string?>
            {
                ["sku"] = row.Sku,
                ["categoryName"] = row.CategoryName
            },
            Children: [])).ToList();

        var unitCount = lineRows.Sum(row => (long)row.Quantity);
        var linesTotal = lineRows.Sum(row => row.LineTotal);

        var summaryNodes = new List<OrdersTreeNode>
        {
            MetricNode(orderId, "lineCount", lineRows.Count),
            MetricNode(orderId, "unitCount", unitCount),
            MetricNode(orderId, "linesTotal", linesTotal)
        };

        var groups = new List<OrdersTreeNode>
        {
            new(
                Id: $"o:{orderId}:lines",
                NodeType: "orderLineGroup",
                Label: header.OrderNumber,
                NextLevel: null,
                Coordinates: new OrdersTreeCoordinates(
                    Year: coordinates.Year, Month: coordinates.Month,
                    CategoryId: coordinates.CategoryId, ProductId: coordinates.ProductId, OrderId: orderId),
                Metrics: new OrdersTreeMetrics(LineCount: lineRows.Count, UnitCount: unitCount, TotalAmount: linesTotal),
                Details: EmptyDetails,
                Children: lineNodes),
            new(
                Id: $"o:{orderId}:summary",
                NodeType: "orderSummaryGroup",
                Label: header.OrderNumber,
                NextLevel: null,
                Coordinates: new OrdersTreeCoordinates(OrderId: orderId),
                Metrics: new OrdersTreeMetrics(TotalAmount: header.TotalAmount),
                Details: new Dictionary<string, string?>
                {
                    ["customerName"] = header.CustomerName,
                    ["status"] = header.Status,
                    ["placedAtUtc"] = header.PlacedAtUtc.ToString("O", CultureInfo.InvariantCulture)
                },
                Children: summaryNodes)
        };

        return Complete(OrdersTreeLevels.Leaf, groups);
    }

    private static OrdersTreeNode MetricNode(long orderId, string metric, decimal value) => new(
        Id: $"o:{orderId}:summary:{metric}",
        NodeType: "orderMetric",
        Label: value.ToString(CultureInfo.InvariantCulture),
        NextLevel: null,
        Coordinates: new OrdersTreeCoordinates(OrderId: orderId),
        Metrics: new OrdersTreeMetrics(TotalAmount: value),
        Details: new Dictionary<string, string?> { ["metric"] = metric },
        Children: []);

    // --- Exportacion -----------------------------------------------------------------------

    public async Task<int> GetExportCountAsync(
        OrdersTreeSearch search, OrdersTreeCoordinates coordinates, CancellationToken cancellationToken)
    {
        var parameters = ExportParameters(search, coordinates);
        return await db.ExecuteScalarAsync<int>(
            OrdersTreeSql.ExportCount(search.SearchType), parameters,
            commandTimeout: ExportCommandTimeoutSeconds, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IReadOnlyList<object?>>> GetExportPageAsync(
        OrdersTreeSearch search, OrdersTreeCoordinates coordinates, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var parameters = ExportParameters(search, coordinates);
        parameters.Add("Offset", (pageNumber - 1) * pageSize, DbType.Int32);
        parameters.Add("PageSize", pageSize, DbType.Int32);

        var rows = await db.QueryAsync<ExportRow>(
            OrdersTreeSql.ExportPage(search.SearchType), parameters,
            commandTimeout: ExportCommandTimeoutSeconds, cancellationToken: cancellationToken).ConfigureAwait(false);

        // Los valores viajan como valores, no como texto ya formateado: el formato de fecha y
        // de moneda depende del locale del usuario, y ese lo conoce el cliente.
        return rows.Select(row => (IReadOnlyList<object?>)new object?[]
        {
            row.OrderNumber,
            row.PlacedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            row.CustomerName,
            row.Status,
            row.CategoryName,
            row.Sku,
            row.ProductName,
            row.Quantity,
            row.UnitPrice,
            row.LineTotal
        }).ToList();
    }

    // --- Parametros ------------------------------------------------------------------------

    private static readonly IReadOnlyDictionary<string, string?> EmptyDetails =
        new Dictionary<string, string?>();

    /// <summary>
    /// Punto unico donde se arman los parametros. El tenant lo agrega TenantScope.Bind desde el
    /// contexto de la peticion: aqui no hay forma de pasarle otro valor.
    /// </summary>
    private DynamicParameters BaseParameters(OrdersTreeSearch search)
    {
        var parameters = tenantScope.Bind();

        // El valor de busqueda solo se agrega si el fragmento elegido lo referencia: mandar un
        // parametro que el SQL no usa es un error en PostgreSQL, no un descuido inofensivo.
        if (OrdersTreeSql.SearchPredicate(search.SearchType).Contains("@SearchValue", StringComparison.Ordinal))
            parameters.Add("SearchValue", search.SearchValue ?? string.Empty, DbType.String);

        return parameters;
    }

    private static void AddPeriod(DynamicParameters parameters, int year, int? month)
    {
        var start = month is null
            ? new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            : new DateTime(year, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = month is null ? start.AddYears(1) : start.AddMonths(1);

        parameters.Add("PeriodStart", start, DbType.DateTime);
        parameters.Add("PeriodEnd", end, DbType.DateTime);
    }

    private DynamicParameters ExportParameters(OrdersTreeSearch search, OrdersTreeCoordinates coordinates)
    {
        var parameters = BaseParameters(search);

        // Sin coordenadas de periodo la ventana es abierta, pero SIEMPRE es un rango: un
        // parametro nulo de fecha obliga a Npgsql a adivinar el tipo y ademas rompe el uso del
        // indice. Los identificadores si van nulos, con su DbType explicito.
        var (start, end) = ResolvePeriod(coordinates);
        parameters.Add("PeriodStart", start, DbType.DateTime);
        parameters.Add("PeriodEnd", end, DbType.DateTime);
        parameters.Add("CategoryId", coordinates.CategoryId, DbType.Int64);
        parameters.Add("ProductId", coordinates.ProductId, DbType.Int64);
        parameters.Add("OrderId", coordinates.OrderId, DbType.Int64);

        return parameters;
    }

    private static (DateTime Start, DateTime End) ResolvePeriod(OrdersTreeCoordinates coordinates)
    {
        if (coordinates.Year is null)
            return (new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(9999, 12, 31, 0, 0, 0, DateTimeKind.Utc));

        var start = coordinates.Month is null
            ? new DateTime(coordinates.Year.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            : new DateTime(coordinates.Year.Value, coordinates.Month.Value, 1, 0, 0, 0, DateTimeKind.Utc);

        return (start, coordinates.Month is null ? start.AddYears(1) : start.AddMonths(1));
    }

    private static OrdersTreeChildren Complete(string level, IReadOnlyList<OrdersTreeNode> items) =>
        // Los niveles sin fan-out van completos: meter paginacion en un nivel de doce filas es
        // complejidad que no compra nada.
        new(level, items, items.Count, PageNumber: 1, PageSize: items.Count, HasMore: false);
}
