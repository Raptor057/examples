namespace Orders.Domain.Trees;

/// <summary>
/// Criterio de busqueda del arbol completo. El tipo elige un fragmento fijo de SQL; el valor
/// viaja siempre parametrizado.
/// </summary>
public sealed record OrdersTreeSearch(string SearchType, string? SearchValue);

/// <summary>
/// Coordenadas de un nodo. Cada nodo carga las suyas y el cliente las reenvia tal cual al
/// expandirlo: nunca las reconstruye caminando padres hacia arriba, que es de donde sale el bug
/// clasico de una rama cargada por otro camino con coordenadas incompletas.
/// </summary>
public sealed record OrdersTreeCoordinates(
    int? Year = null,
    int? Month = null,
    long? CategoryId = null,
    long? ProductId = null,
    long? OrderId = null);

/// <summary>
/// Numeros agregados del nodo, calculados por el motor. Viajan como NUMEROS, no como texto ya
/// compuesto: "1,204 pedidos" es una cadena traducible y formateada por locale, y eso es trabajo
/// del cliente. El servidor manda el dato; el cliente lo redacta.
/// </summary>
public sealed record OrdersTreeMetrics(
    long? OrderCount = null,
    decimal? TotalAmount = null,
    long? LineCount = null,
    long? UnitCount = null,
    decimal? UnitPrice = null);

/// <summary>
/// Nodo del arbol. NextLevel es lo que hace que el cliente no conozca la jerarquia: null
/// significa hoja, y el cliente sabe que no hay flecha de expandir sin preguntar.
/// </summary>
public sealed record OrdersTreeNode(
    string Id,
    string NodeType,
    string Label,
    string? NextLevel,
    OrdersTreeCoordinates Coordinates,
    OrdersTreeMetrics Metrics,
    IReadOnlyDictionary<string, string?> Details,
    IReadOnlyList<OrdersTreeNode> Children);

public sealed record OrdersTreeChildren(
    string Level,
    IReadOnlyList<OrdersTreeNode> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    bool HasMore);
