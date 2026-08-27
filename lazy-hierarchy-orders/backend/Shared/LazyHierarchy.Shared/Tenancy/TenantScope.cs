using System.Text.RegularExpressions;
using Dapper;
using LazyHierarchy.Kernel;

namespace LazyHierarchy.Shared.Tenancy;

/// <summary>
/// El precio de admitir Dapper en un proyecto con tenancy.
///
/// Con EF Core el aislamiento lo aplica el global query filter y no hay nada que olvidar.
/// Las consultas del arbol perezoso bajan a SQL crudo porque EF no expresa bien un CTE que
/// pagina primero y un LEFT JOIN LATERAL que enriquece despues; a cambio, el filtro de tenant
/// deja de ser automatico. La salida NO es escribir el WHERE a mano en cada consulta -eso es
/// justo el antipatron que prohibe la regla de oro 2- sino concentrarlo aqui:
///
///   1. <see cref="On(string)"/> es el UNICO lugar donde se escribe el predicado de tenant.
///   2. <see cref="Bind"/> es el UNICO lugar donde se pone su valor, y lo toma del contexto
///      de la peticion: el tenant jamas llega por query string ni por el cuerpo.
///   3. Una prueba automatizada recorre el SQL de todos los niveles y falla si alguno
///      referencia una tabla de negocio sin el fragmento canonico. Sin esa red, el patron es
///      una fuga esperando a ocurrir.
/// </summary>
public sealed class TenantScope(ITenantContextAccessor accessor)
{
    /// <summary>Nombre del parametro. Nunca se interpola el valor en el SQL.</summary>
    public const string ParameterName = "TenantId";

    /// <summary>Columna de aislamiento, presente en toda tabla de negocio.</summary>
    public const string Column = "tenant_id";

    /// <summary>Fragmento canonico para un alias. Ninguna consulta escribe este texto a mano.</summary>
    public static string On(string alias) => $"{alias}.{Column} = @{ParameterName}";

    /// <summary>Fragmento canonico para varios alias, unidos por AND.</summary>
    public static string On(params string[] aliases) => string.Join(" AND ", aliases.Select(On));

    /// <summary>Reconoce el fragmento canonico. La usa la prueba de aislamiento.</summary>
    public static readonly Regex CanonicalPredicate =
        new(@"\b\w+\.tenant_id\s*=\s*@TenantId\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Reconoce CUALQUIER mencion de la columna. La usa la prueba de aislamiento.</summary>
    public static readonly Regex AnyTenantColumn =
        new(@"\btenant_id\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Tenant vigente. Sin contexto vale el contexto sistema: fail-closed.</summary>
    public long CurrentTenantId => accessor.Current?.TenantId ?? SystemContext.TenantId;

    /// <summary>
    /// Agrega el parametro de tenant a los parametros de la consulta. Es la unica via por la
    /// que el repositorio obtiene ese valor: no lo recibe del handler ni del request.
    /// </summary>
    public DynamicParameters Bind(object? parameters = null)
    {
        var bound = parameters is null ? new DynamicParameters() : new DynamicParameters(parameters);
        bound.Add(ParameterName, CurrentTenantId);
        return bound;
    }
}
