using System.Text.RegularExpressions;
using Audit.Infrastructure.Persistence.Sql.MainDb;
using SaasAccessAudit.Shared.Tenancy;

namespace Audit.Tests;

/// <summary>
/// LA PRUEBA QUE JUSTIFICA USAR DAPPER EN UN PROYECTO CON TENANCY.
///
/// La regla de oro dice que donde hay aislamiento que olvidar, lo aplica el ORM, y lista como
/// antipatron usar Dapper con tenancy filtrando el tenant a mano en cada consulta. Este ejemplo
/// admite una excepcion ACOTADA -las dos consultas paginadas de la bitacora- y el precio de esa
/// excepcion es exactamente esto: una prueba que recorre TODAS las variantes del SQL y falla si
/// alguna referencia una tabla sin el fragmento canonico de tenant.
///
/// Sin esta red el patron es una fuga esperando a ocurrir: basta que alguien agregue un filtro
/// nuevo y se le vaya el predicado para que un tenant lea la bitacora de otro, sin error y sin
/// sintoma. Y en una bitacora la fuga es de las peores: quien la lea vera lo que hizo la
/// competencia, con nombres y motivos.
///
/// Son 64 y 32 variantes porque los filtros son opcionales y cada combinacion produce un SQL
/// distinto. Probar solo "sin filtros" comprobaria la unica consulta que nadie escribe a mano.
/// </summary>
public sealed class TenantIsolationSqlTests
{
    /// <summary>Tablas cuyas filas pertenecen a un tenant.</summary>
    private static readonly HashSet<string> BusinessTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "action_audit_entry", "access_denial_entry"
    };

    /// <summary>Captura los pares (tabla, alias) de cada FROM y cada JOIN.</summary>
    private static readonly Regex TableAlias =
        new(@"\b(?:FROM|JOIN)\s+(\w+)\s+(\w+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static TheoryData<string, string> AllStatements()
    {
        var data = new TheoryData<string, string>();

        for (var index = 0; index < AuditSql.AllActionShapes.Count; index++)
            data.Add($"actions[{index}]", AuditSql.ActionsPage(AuditSql.AllActionShapes[index]));

        for (var index = 0; index < AuditSql.AllDenialShapes.Count; index++)
            data.Add($"denials[{index}]", AuditSql.DenialsPage(AuditSql.AllDenialShapes[index]));

        return data;
    }

    /// <summary>
    /// Devuelve las tablas que la consulta referencia SIN el fragmento de tenant. Esta extraida
    /// para que la propia deteccion se pueda probar: un guardia que no puede fallar no protege
    /// de nada.
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
        const string unscoped = """
            SELECT a.public_id
            FROM   action_audit_entry a
            JOIN   access_denial_entry d ON d.permission_code = a.action_code
            WHERE  a.action_code = @ActionCode
            """;

        var missing = FindUnscopedTables(unscoped);

        Assert.Equal(2, missing.Count);
        Assert.Contains("action_audit_entry AS a", missing);
        Assert.Contains("access_denial_entry AS d", missing);
    }

    [Fact]
    public void La_deteccion_acepta_la_consulta_ya_aislada()
    {
        var scoped = $"""
            SELECT a.public_id
            FROM   action_audit_entry a
            WHERE  {TenantScope.On("a")}
            """;

        Assert.Empty(FindUnscopedTables(scoped));
    }

    [Theory]
    [MemberData(nameof(AllStatements))]
    public void Toda_tabla_lleva_el_fragmento_de_tenant(string name, string sql)
    {
        var missing = FindUnscopedTables(sql);

        Assert.True(
            missing.Count == 0,
            $"La consulta '{name}' referencia tablas sin el fragmento de tenant: {string.Join(", ", missing)}. "
            + "Componelo con TenantScope.On(alias); no lo escribas a mano.");
    }

    [Theory]
    [MemberData(nameof(AllStatements))]
    public void Toda_mencion_de_tenant_id_esta_en_forma_canonica(string name, string sql)
    {
        // Si alguien escribe el filtro a mano -o lo escribe distinto- las dos cuentas dejan de
        // cuadrar. Es lo que impide que el fragmento centralizado se vuelva opcional.
        var mentions = TenantScope.AnyTenantColumn.Matches(sql).Count;
        var canonical = TenantScope.CanonicalPredicate.Matches(sql).Count;

        Assert.True(
            mentions == canonical,
            $"La consulta '{name}' menciona tenant_id {mentions} veces pero solo {canonical} en la forma "
            + "'alias.tenant_id = @TenantId'. El filtro se compone con TenantScope, no se escribe a mano.");
    }

    [Theory]
    [MemberData(nameof(AllStatements))]
    public void Toda_consulta_usa_el_parametro_de_tenant(string name, string sql) =>
        Assert.True(
            sql.Contains($"@{TenantScope.ParameterName}", StringComparison.Ordinal),
            $"La consulta '{name}' no usa el parametro de tenant.");

    [Fact]
    public void Se_recorren_todas_las_variantes_de_filtro()
    {
        // Seis filtros opcionales en acciones y cinco en accesos: 64 y 32 combinaciones. Si el
        // generador dejara de producirlas, las pruebas de arriba pasarian comprobando una sola.
        Assert.Equal(64, AuditSql.AllActionShapes.Count);
        Assert.Equal(32, AuditSql.AllDenialShapes.Count);
        Assert.Equal(AuditSql.AllActionShapes.Count, AuditSql.AllActionShapes.Distinct().Count());
    }
}
