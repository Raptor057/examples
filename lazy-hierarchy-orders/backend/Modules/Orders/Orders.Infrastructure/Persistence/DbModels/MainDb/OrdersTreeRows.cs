namespace Orders.Infrastructure.Persistence.DbModels.MainDb;

// La forma EXACTA de la fila que devuelve cada consulta. No son entidades de dominio: llevan
// campos que solo existen para el transporte (TotalCount de la consulta paginada) y tipos tal
// como los entrega el motor. El repositorio mapea Row -> modelo de dominio; el dominio nunca ve
// un Row. Mezclarlos ata el dominio a la forma de la tabla.
//
// Las columnas vienen en snake_case y las propiedades en PascalCase: el puente lo hace
// Dapper.DefaultTypeMap.MatchNamesWithUnderscores, encendido una sola vez en el registro de la
// capa compartida. La alternativa seria entrecomillar alias en el SQL, que es justo lo que
// PostgreSQL no quiere.

internal sealed record TreeYearRow
{
    public int YearNumber { get; init; }

    public long OrderCount { get; init; }

    public decimal TotalAmount { get; init; }
}

internal sealed record TreeMonthRow
{
    public int MonthNumber { get; init; }

    public long OrderCount { get; init; }

    public decimal TotalAmount { get; init; }
}

internal sealed record TreeCategoryRow
{
    public long CategoryId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public long OrderCount { get; init; }

    public decimal TotalAmount { get; init; }
}

internal sealed record TreeProductRow
{
    public long ProductId { get; init; }

    public string Sku { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public long OrderCount { get; init; }

    public decimal TotalAmount { get; init; }
}

internal sealed record TreeOrderRow
{
    public long OrderId { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime PlacedAtUtc { get; init; }

    public decimal TotalAmount { get; init; }

    /// <summary>Total real de la consulta, calculado por COUNT(*) OVER() dentro del CTE.</summary>
    public int TotalCount { get; init; }

    public long LineCount { get; init; }

    public long UnitCount { get; init; }

    public long FocusUnitCount { get; init; }

    public decimal FocusAmount { get; init; }
}

internal sealed record TreeOrderHeaderRow
{
    public long OrderId { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime PlacedAtUtc { get; init; }

    public decimal TotalAmount { get; init; }
}

internal sealed record TreeOrderLineRow
{
    public long LineId { get; init; }

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal LineTotal { get; init; }

    public string Sku { get; init; } = string.Empty;

    public string ProductName { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;
}

internal sealed record ExportRow
{
    public string OrderNumber { get; init; } = string.Empty;

    public DateTime PlacedAtUtc { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string CategoryName { get; init; } = string.Empty;

    public string Sku { get; init; } = string.Empty;

    public string ProductName { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal LineTotal { get; init; }
}
