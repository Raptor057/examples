namespace Orders.Application.Dtos;

/// <summary>
/// Nodo tal como lo ve el cliente. Las coordenadas van PLANAS a proposito: al expandir, el
/// cliente reenvia estos mismos campos como parametros de query, sin reconstruir nada.
/// </summary>
public sealed record OrdersTreeNodeDto(
    string Id,
    string NodeType,
    string Label,
    string? NextLevel,
    int? Year,
    int? Month,
    long? CategoryId,
    long? ProductId,
    long? OrderId,
    OrdersTreeMetricsDto Metrics,
    IReadOnlyDictionary<string, string?> Details,
    IReadOnlyList<OrdersTreeNodeDto> Children);

public sealed record OrdersTreeMetricsDto(
    long? OrderCount,
    decimal? TotalAmount,
    long? LineCount,
    long? UnitCount,
    decimal? UnitPrice);

public sealed record OrdersTreeChildrenDto(
    string Level,
    IReadOnlyList<OrdersTreeNodeDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    bool HasMore);

/// <summary>
/// Columna de la exportacion. Viaja la LLAVE, no el encabezado: el titulo traducido lo pone el
/// cliente con su idioma activo, igual que las columnas de la pantalla.
/// </summary>
public sealed record OrdersExportColumnDto(string Key, string Type);

public sealed record OrdersExportPageDto(
    IReadOnlyList<OrdersExportColumnDto> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

public sealed record CreatedOrderDto(Guid PublicId, string OrderNumber, decimal TotalAmount, int LineCount);
