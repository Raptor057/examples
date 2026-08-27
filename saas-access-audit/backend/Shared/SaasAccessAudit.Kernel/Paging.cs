namespace SaasAccessAudit.Kernel;

/// <summary>
/// Una pagina y el TOTAL real del conjunto filtrado.
///
/// El total no es decorado: sin el, la pantalla no puede dibujar el paginador ni decirle a nadie
/// cuantos renglones cumplen el filtro, y la unica alternativa es traerlo todo para contarlo.
/// Por eso las consultas lo calculan con COUNT(1) OVER() en la misma pasada, y por eso en este
/// proyecto no hay ni un TOP: se pagina de verdad, con OFFSET/FETCH.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, long TotalCount, int PageNumber, int PageSize)
{
    public static PagedResult<T> Empty(int pageNumber, int pageSize) =>
        new([], 0, pageNumber, pageSize);
}

/// <summary>
/// Reglas de paginacion compartidas. Viven en un solo lugar porque el tope de tamano de pagina
/// es una defensa, no una preferencia: un pageSize que llega del cliente sin validar convierte
/// cualquier pantalla en una descarga completa de la tabla.
/// </summary>
public static class Paging
{
    public const int DefaultPageSize = 25;

    public const int MaxPageSize = 200;

    public static int NormalizePageNumber(int? pageNumber) =>
        pageNumber is null or < 1 ? 1 : pageNumber.Value;

    public static int NormalizePageSize(int? pageSize) => pageSize switch
    {
        null or < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => pageSize.Value
    };

    /// <summary>Desplazamiento de la pagina. Viaja como PARAMETRO, no se arma dentro del SQL.</summary>
    public static int Offset(int pageNumber, int pageSize) => (pageNumber - 1) * pageSize;
}
