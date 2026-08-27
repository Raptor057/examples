using System.Text.RegularExpressions;
using LazyHierarchy.Shared.Tenancy;
using Orders.Domain.Trees;
using Orders.Infrastructure.Persistence.Sql.MainDb;

namespace Orders.Tests;

/// <summary>
/// LA PRUEBA QUE JUSTIFICA USAR DAPPER EN UN PROYECTO CON TENANCY.
///
/// La regla de oro dice que donde hay aislamiento que olvidar, lo aplica el ORM. Este ejemplo
/// admite una excepcion acotada -las consultas del arbol, que EF no expresa bien- y el precio de
/// esa excepcion es exactamente esto: una prueba que recorre TODAS las consultas por nivel y
/// falla si alguna referencia una tabla de negocio sin el fragmento canonico de tenant.
///
/// Sin esta red, el patron es una fuga esperando a ocurrir: basta que alguien agregue un JOIN y
/// se le olvide el predicado para que un tenant vea datos de otro, sin error y sin sintoma.
/// </summary>
public sealed class TenantIsolationSqlTests
{
    /// <summary>Tablas cuyas filas pertenecen a un tenant. La tabla tenant NO esta: es root.</summary>
    private static readonly HashSet<string> BusinessTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "category", "product", "customer_order", "order_line"
    };

    /// <summary>Captura los pares (tabla, alias) de cada FROM y cada JOIN.</summary>
    private static readonly Regex TableAlias =
        new(@"\b(?:FROM|JOIN)\s+(\w+)\s+(\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static TheoryData<string, string> AllStatements()
    {
        var data = new TheoryData<string, string>();
        foreach (var level in OrdersTreeLevels.Allowed)
        {
            foreach (var searchType in OrdersTreeSql.SearchTypes)
            {
                var statements = OrdersTreeSql.StatementsFor(level, searchType);
                for (var i = 0; i < statements.Count; i++)
                    data.Add($"{level}[{searchType}][{i}]", statements[i]);
            }
        }

        foreach (var searchType in OrdersTreeSql.SearchTypes)
        {
            data.Add($"export-count[{searchType}]", OrdersTreeSql.ExportCount(searchType));
            data.Add($"export-page[{searchType}]", OrdersTreeSql.ExportPage(searchType));
        }

        return data;
    }

    /// <summary>
    /// Devuelve las tablas de negocio que la consulta referencia SIN el fragmento de tenant.
    /// Esta extraida para que la propia deteccion se pueda probar: un guardia que no puede
    /// fallar no protege de nada.
    /// </summary>
    private static IReadOnlyList<string> FindUnscopedTables(string sql)
    {
        var missing = new List<string>();

        foreach (Match match in TableAlias.Matches(sql))
        {
            var table = match.Groups[1].Value;
            var alias = match.Groups[2].Value;
            if (!BusinessTables.Contains(table)) continue;

            if (!sql.Contains(TenantScope.On(alias), StringComparison.OrdinalIgnoreCase))
                missing.Add($"{table} AS {alias}");
        }

        return missing;
    }

    [Fact]
    public void La_deteccion_encuentra_una_consulta_sin_aislar()
    {
        // Prueba de la prueba. Si el reconocedor de alias dejara de encontrar tablas, todas las
        // demas pasarian en verde sin comprobar nada.
        const string unscoped = """
            SELECT ol.id
            FROM   order_line ol
            JOIN   customer_order o ON o.id = ol.order_id
            WHERE  ol.is_active = true
            """;

        var missing = FindUnscopedTables(unscoped);

        Assert.Equal(2, missing.Count);
        Assert.Contains("order_line AS ol", missing);
        Assert.Contains("customer_order AS o", missing);
    }

    [Fact]
    public void La_deteccion_acepta_la_consulta_ya_aislada()
    {
        var scoped = $"""
            SELECT ol.id
            FROM   order_line ol
            JOIN   customer_order o ON o.id = ol.order_id AND {TenantScope.On("o")}
            WHERE  {TenantScope.On("ol")} AND ol.is_active = true
            """;

        Assert.Empty(FindUnscopedTables(scoped));
    }

    [Theory]
    [MemberData(nameof(AllStatements))]
    public void Toda_tabla_de_negocio_lleva_el_fragmento_de_tenant(string name, string sql)
    {
        var missing = FindUnscopedTables(sql);

        Assert.True(
            missing.Count == 0,
            $"La consulta '{name}' referencia tablas de negocio sin el fragmento de tenant: {string.Join(", ", missing)}. " +
            $"Componelo con TenantScope.On(alias); no lo escribas a mano.");
    }

    [Theory]
    [MemberData(nameof(AllStatements))]
    public void Toda_mencion_de_tenant_id_esta_en_forma_canonica(string name, string sql)
    {
        // Si alguien escribe el filtro a mano (o lo escribe distinto), las dos cuentas dejan de
        // cuadrar. Es lo que impide que el fragmento centralizado se vuelva opcional.
        var mentions = TenantScope.AnyTenantColumn.Matches(sql).Count;
        var canonical = TenantScope.CanonicalPredicate.Matches(sql).Count;

        Assert.True(
            mentions == canonical,
            $"La consulta '{name}' menciona tenant_id {mentions} veces pero solo {canonical} en la forma " +
            $"'alias.tenant_id = @TenantId'. El filtro de tenant se compone con TenantScope, no se escribe a mano.");
    }

    [Theory]
    [MemberData(nameof(AllStatements))]
    public void Toda_consulta_usa_el_parametro_de_tenant(string name, string sql)
    {
        Assert.True(
            sql.Contains($"@{TenantScope.ParameterName}", StringComparison.Ordinal),
            $"La consulta '{name}' no usa el parametro de tenant.");
    }

    [Fact]
    public void El_predicado_de_busqueda_desconocido_es_fail_closed()
    {
        // El default es 1 = 0 y no 1 = 1: un tipo de busqueda que nadie contemplo devuelve vacio,
        // no la tabla entera. El fallo silencioso mas caro de este patron es el que abre.
        Assert.Equal("1 = 0", OrdersTreeSql.SearchPredicate("cualquier-cosa"));
        Assert.Equal("1 = 0", OrdersTreeSql.SearchPredicate(string.Empty));
        Assert.Equal("1 = 0", OrdersTreeSql.SearchPredicate("' OR 1=1 --"));
    }

    [Theory]
    [InlineData("status")]
    [InlineData("customer")]
    [InlineData("ordernumber")]
    public void El_valor_de_busqueda_viaja_parametrizado(string searchType)
    {
        // El tipo elige un fragmento fijo; el VALOR nunca entra al texto del SQL. Por eso la
        // firma de SearchPredicate ni siquiera recibe el valor: interpolarlo seria imposible.
        Assert.Contains("@SearchValue", OrdersTreeSql.SearchPredicate(searchType), StringComparison.Ordinal);
    }

    [Fact]
    public void El_nivel_fuera_del_conjunto_cerrado_no_produce_sql()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OrdersTreeSql.StatementsFor("drop-table", "all"));
    }
}
