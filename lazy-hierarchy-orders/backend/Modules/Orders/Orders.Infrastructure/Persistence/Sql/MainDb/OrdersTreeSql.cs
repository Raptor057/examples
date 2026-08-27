using System.Globalization;
using LazyHierarchy.Shared.Tenancy;
using Orders.Domain.Trees;

namespace Orders.Infrastructure.Persistence.Sql.MainDb;

/// <summary>
/// Todo el SQL del arbol, en un solo lugar y siempre parametrizado.
///
/// Dos decisiones que no son cosmeticas:
///
/// 1. El filtro de tenant NUNCA se escribe aqui: se compone con <see cref="TenantScope.On(string)"/>.
///    Es lo que permite que una prueba recorra estas consultas y falle si alguna toca una tabla
///    de negocio sin aislar.
/// 2. El tipo de busqueda elige un FRAGMENTO FIJO de SQL; el valor viaja parametrizado. El
///    default es "1 = 0", no "1 = 1": un tipo que nadie contemplo devuelve vacio, no la tabla
///    entera. El fallo silencioso mas caro de este patron es el que abre.
/// </summary>
internal static class OrdersTreeSql
{
    /// <summary>Tipos de busqueda soportados. Cualquier otro cae en el default fail-closed.</summary>
    public static readonly IReadOnlySet<string> SearchTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "all", "status", "customer", "ordernumber"
    };

    /// <summary>
    /// Predicado sobre el pedido (alias o). Switch cerrado: el valor jamas se interpola, solo
    /// se elige entre fragmentos que ya existen en el codigo.
    /// </summary>
    public static string SearchPredicate(string searchType) => searchType.ToLowerInvariant() switch
    {
        "all" => "1 = 1",
        "status" => "o.status = @SearchValue",
        "customer" => "o.customer_name ILIKE '%' || @SearchValue || '%'",
        "ordernumber" => "o.order_number = @SearchValue",
        _ => "1 = 0"
    };

    /// <summary>
    /// Todas las sentencias que ejecuta un nivel. La hoja usa dos (cabecera y lineas) porque
    /// devuelve su subarbol completo en una sola llamada del cliente.
    /// </summary>
    public static IReadOnlyList<string> StatementsFor(string level, string searchType) => level switch
    {
        OrdersTreeLevels.Year => [Years(searchType)],
        OrdersTreeLevels.Month => [Months(searchType)],
        OrdersTreeLevels.Category => [Categories(searchType)],
        OrdersTreeLevels.Product => [Products(searchType)],
        OrdersTreeLevels.Order => [OrdersPage(searchType)],
        OrdersTreeLevels.Leaf => [LeafHeader(searchType), LeafLines(searchType)],
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Nivel fuera del conjunto cerrado.")
    };

    // --- Niveles de agregacion -------------------------------------------------------------
    // El numero que se ve junto al nodo NO sale de contar hijos en memoria: lo calcula el motor
    // sin traerlos. Eso es lo que hace que cada expansion pese cientos de bytes y no megabytes.

    /// <summary>Nivel 1. Un renglon por anio con su conteo de pedidos.</summary>
    public static string Years(string searchType) => $"""
        SELECT   date_part('year', o.placed_at_utc)::int AS year_number,
                 COUNT(*)                                AS order_count,
                 COALESCE(SUM(o.total_amount), 0)        AS total_amount
        FROM     customer_order o
        WHERE    {TenantScope.On("o")}
          AND    o.is_active = true
          AND    ({SearchPredicate(searchType)})
        GROUP BY year_number
        ORDER BY year_number DESC
        """;

    /// <summary>
    /// Nivel 2. El periodo entra como rango sobre la columna y no como funcion aplicada a ella:
    /// date_part(...) = @Year en el WHERE dejaria el indice inservible.
    /// </summary>
    public static string Months(string searchType) => $"""
        SELECT   date_part('month', o.placed_at_utc)::int AS month_number,
                 COUNT(*)                                 AS order_count,
                 COALESCE(SUM(o.total_amount), 0)         AS total_amount
        FROM     customer_order o
        WHERE    {TenantScope.On("o")}
          AND    o.is_active = true
          AND    o.placed_at_utc >= @PeriodStart
          AND    o.placed_at_utc <  @PeriodEnd
          AND    ({SearchPredicate(searchType)})
        GROUP BY month_number
        ORDER BY month_number
        """;

    /// <summary>Nivel 3. Categorias con pedidos en el periodo.</summary>
    public static string Categories(string searchType) => $"""
        SELECT   c.id                             AS category_id,
                 c.code                           AS code,
                 c.name                           AS name,
                 COUNT(DISTINCT ol.order_id)      AS order_count,
                 COALESCE(SUM(ol.line_total), 0)  AS total_amount
        FROM     order_line ol
        JOIN     customer_order o ON o.id = ol.order_id   AND {TenantScope.On("o")} AND o.is_active = true
        JOIN     product p        ON p.id = ol.product_id AND {TenantScope.On("p")} AND p.is_active = true
        JOIN     category c       ON c.id = p.category_id AND {TenantScope.On("c")} AND c.is_active = true
        WHERE    {TenantScope.On("ol")}
          AND    ol.is_active = true
          AND    o.placed_at_utc >= @PeriodStart
          AND    o.placed_at_utc <  @PeriodEnd
          AND    ({SearchPredicate(searchType)})
        GROUP BY c.id, c.code, c.name
        ORDER BY c.name
        """;

    /// <summary>Nivel 4. Productos de una categoria con pedidos en el periodo.</summary>
    public static string Products(string searchType) => $"""
        SELECT   p.id                             AS product_id,
                 p.sku                            AS sku,
                 p.name                           AS name,
                 COUNT(DISTINCT ol.order_id)      AS order_count,
                 COALESCE(SUM(ol.line_total), 0)  AS total_amount
        FROM     order_line ol
        JOIN     customer_order o ON o.id = ol.order_id   AND {TenantScope.On("o")} AND o.is_active = true
        JOIN     product p        ON p.id = ol.product_id AND {TenantScope.On("p")} AND p.is_active = true
                                 AND p.category_id = @CategoryId
        WHERE    {TenantScope.On("ol")}
          AND    ol.is_active = true
          AND    o.placed_at_utc >= @PeriodStart
          AND    o.placed_at_utc <  @PeriodEnd
          AND    ({SearchPredicate(searchType)})
        GROUP BY p.id, p.sku, p.name
        ORDER BY p.name
        """;

    // --- El nivel pesado -------------------------------------------------------------------

    /// <summary>
    /// Nivel 5, el unico con fan-out grande y por eso el unico paginado.
    ///
    /// La idea central: PAGINAR PRIMERO con la consulta mas barata posible (un CTE que solo
    /// toca customer_order y resuelve la pertenencia al producto por EXISTS, que no multiplica
    /// filas y corta en cuanto encuentra una), y solo despues colgar lo caro con
    /// LEFT JOIN LATERAL, que corre por fila de la PAGINA y no por fila de la tabla. Poner esos
    /// subqueries como JOIN antes de paginar los ejecutaria sobre el universo completo.
    ///
    /// COUNT(*) OVER() da el total real, y el ORDER BY termina en la llave primaria para que sea
    /// UNICO: sin eso el paginado repite y omite filas.
    ///
    /// Traduccion desde SQL Server: OUTER APPLY se escribe LEFT JOIN LATERAL ... ON TRUE, y el
    /// hint WITH (NOLOCK) no se traduce, se borra: PostgreSQL es MVCC y las lecturas no bloquean.
    /// </summary>
    public static string OrdersPage(string searchType) => $"""
        WITH page AS (
            SELECT   o.id            AS order_id,
                     o.order_number  AS order_number,
                     o.customer_name AS customer_name,
                     o.status        AS status,
                     o.placed_at_utc AS placed_at_utc,
                     o.total_amount  AS total_amount,
                     COUNT(*) OVER() AS total_count
            FROM     customer_order o
            WHERE    {TenantScope.On("o")}
              AND    o.is_active = true
              AND    o.placed_at_utc >= @PeriodStart
              AND    o.placed_at_utc <  @PeriodEnd
              AND    ({SearchPredicate(searchType)})
              AND    EXISTS (
                         SELECT 1
                         FROM   order_line ol
                         WHERE  ol.order_id = o.id
                           AND  ol.product_id = @ProductId
                           AND  {TenantScope.On("ol")}
                           AND  ol.is_active = true)
            ORDER BY o.placed_at_utc DESC, o.id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
        )
        SELECT   pg.order_id, pg.order_number, pg.customer_name, pg.status,
                 pg.placed_at_utc, pg.total_amount, pg.total_count,
                 whole.line_count, whole.unit_count,
                 focus.focus_unit_count, focus.focus_amount
        FROM     page pg
        LEFT JOIN LATERAL (
                     SELECT COUNT(*)                        AS line_count,
                            COALESCE(SUM(ol.quantity), 0)   AS unit_count
                     FROM   order_line ol
                     WHERE  ol.order_id = pg.order_id
                       AND  {TenantScope.On("ol")}
                       AND  ol.is_active = true
                 ) whole ON TRUE
        LEFT JOIN LATERAL (
                     SELECT COALESCE(SUM(ol.quantity), 0)   AS focus_unit_count,
                            COALESCE(SUM(ol.line_total), 0) AS focus_amount
                     FROM   order_line ol
                     WHERE  ol.order_id = pg.order_id
                       AND  ol.product_id = @ProductId
                       AND  {TenantScope.On("ol")}
                       AND  ol.is_active = true
                 ) focus ON TRUE
        ORDER BY pg.placed_at_utc DESC, pg.order_id DESC
        """;

    // --- La hoja ---------------------------------------------------------------------------
    // Devuelve su subarbol completo en una sola llamada del cliente: el detalle de un pedido es
    // chico y siempre se abre entero, asi que un viaje por nivel ahi solo agrega latencia.

    public static string LeafHeader(string searchType) => $"""
        SELECT   o.id            AS order_id,
                 o.order_number  AS order_number,
                 o.customer_name AS customer_name,
                 o.status        AS status,
                 o.placed_at_utc AS placed_at_utc,
                 o.total_amount  AS total_amount
        FROM     customer_order o
        WHERE    o.id = @OrderId
          AND    {TenantScope.On("o")}
          AND    o.is_active = true
          AND    ({SearchPredicate(searchType)})
        """;

    public static string LeafLines(string searchType) => $"""
        SELECT   ol.id          AS line_id,
                 ol.quantity    AS quantity,
                 ol.unit_price  AS unit_price,
                 ol.line_total  AS line_total,
                 p.sku          AS sku,
                 p.name         AS product_name,
                 c.name         AS category_name
        FROM     order_line ol
        JOIN     customer_order o ON o.id = ol.order_id   AND {TenantScope.On("o")} AND o.is_active = true
        JOIN     product p        ON p.id = ol.product_id AND {TenantScope.On("p")} AND p.is_active = true
        JOIN     category c       ON c.id = p.category_id AND {TenantScope.On("c")} AND c.is_active = true
        WHERE    ol.order_id = @OrderId
          AND    {TenantScope.On("ol")}
          AND    ol.is_active = true
          AND    ({SearchPredicate(searchType)})
        ORDER BY ol.id
        """;

    // --- Exportacion -----------------------------------------------------------------------
    // Matriz plana acotada por las MISMAS coordenadas del nodo: un nodo de mes baja el mes, uno
    // de pedido baja el pedido. Cero parametros nuevos, y el usuario ya sabe que va a bajar
    // porque es lo que esta viendo.

    private const string ExportBodyTemplate = """
        FROM     order_line ol
        JOIN     customer_order o ON o.id = ol.order_id   AND {0} AND o.is_active = true
        JOIN     product p        ON p.id = ol.product_id AND {1} AND p.is_active = true
        JOIN     category c       ON c.id = p.category_id AND {2} AND c.is_active = true
        WHERE    {3}
          AND    ol.is_active = true
          AND    o.placed_at_utc >= @PeriodStart
          AND    o.placed_at_utc <  @PeriodEnd
          AND    (@CategoryId  IS NULL OR c.id = @CategoryId)
          AND    (@ProductId   IS NULL OR p.id = @ProductId)
          AND    (@OrderId     IS NULL OR o.id = @OrderId)
          AND    ({4})
        """;

    private static string ExportBody(string searchType) => string.Format(
        CultureInfo.InvariantCulture,
        ExportBodyTemplate,
        TenantScope.On("o"),
        TenantScope.On("p"),
        TenantScope.On("c"),
        TenantScope.On("ol"),
        SearchPredicate(searchType));

    public static string ExportCount(string searchType) => $"""
        SELECT COUNT(*)
        {ExportBody(searchType)}
        """;

    public static string ExportPage(string searchType) => $"""
        SELECT   o.order_number  AS order_number,
                 o.placed_at_utc AS placed_at_utc,
                 o.customer_name AS customer_name,
                 o.status        AS status,
                 c.name          AS category_name,
                 p.sku           AS sku,
                 p.name          AS product_name,
                 ol.quantity     AS quantity,
                 ol.unit_price   AS unit_price,
                 ol.line_total   AS line_total
        {ExportBody(searchType)}
        ORDER BY o.placed_at_utc DESC, o.id DESC, ol.id
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
        """;
}
