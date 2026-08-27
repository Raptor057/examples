namespace Orders.Domain.Trees;

/// <summary>
/// El conjunto cerrado de niveles. "level" llega del cliente, asi que no es una tabla ni un
/// SQL: es una llave de este conjunto. Fuera de el, la peticion no corre nada.
///
/// Jerarquia: anio -&gt; mes -&gt; categoria -&gt; producto -&gt; pedido -&gt; lineas (hoja).
/// </summary>
public static class OrdersTreeLevels
{
    public const string Year = "tree-year";
    public const string Month = "tree-month";
    public const string Category = "tree-category";
    public const string Product = "tree-product";
    public const string Order = "tree-order";
    public const string Leaf = "tree-order-detail";

    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Year, Month, Category, Product, Order, Leaf
    };

    /// <summary>
    /// El servidor decide la jerarquia. El cliente solo reenvia el nextLevel que recibio, y por
    /// eso insertar o reordenar un nivel es un cambio de servidor, no de las dos orillas.
    /// </summary>
    public static string? NextOf(string level) => level switch
    {
        Year => Month,
        Month => Category,
        Category => Product,
        Product => Order,
        Order => Leaf,
        _ => null
    };

    /// <summary>El unico nivel con fan-out grande, y por eso el unico que pagina.</summary>
    public static bool IsPaged(string level) => level == Order;
}
