using Audit.Domain.Models;
using SaasAccessAudit.Shared.Tenancy;

namespace Audit.Infrastructure.Persistence.Sql.MainDb;

/// <summary>
/// Todo el SQL de la bitacora, en un solo lugar y siempre parametrizado.
///
/// Tres decisiones que no son cosmeticas:
///
/// 1. EL FILTRO DE TENANT NUNCA SE ESCRIBE AQUI. Se compone con TenantScope.On(alias). Es lo que
///    permite que una prueba recorra estas consultas -todas sus variantes de filtro- y falle si
///    alguna toca una tabla de bitacora sin aislar. Sin esa red, mezclar Dapper con tenancy es
///    una fuga esperando a ocurrir.
///
/// 2. EL FILTRO ELIGE UN FRAGMENTO FIJO; EL VALOR VIAJA PARAMETRIZADO. Por eso estos metodos
///    reciben la FORMA de los filtros (que esta puesto) y no sus valores: interpolar un valor
///    seria imposible aunque alguien quisiera.
///
/// 3. PAGINADO DE VERDAD, SIN TOP. OFFSET/FETCH mas COUNT(1) OVER() en la misma pasada: la
///    pantalla necesita la pagina Y el total del conjunto filtrado, y calcularlos en dos
///    consultas con predicados copiados es como se llega a que el contador no cuadre con la
///    lista estando las dos bien por separado.
/// </summary>
internal static class AuditSql
{
    /// <summary>
    /// Columnas de la bitacora de acciones. El id no sale: hacia afuera se identifica por
    /// public_id, y el id relacional no viaja en la API.
    /// </summary>
    private const string ActionColumns = """
                 a.public_id,
                 a.action_code,
                 a.subject_type,
                 a.subject_key,
                 a.subject_label,
                 a.reason,
                 a.performed_by,
                 a.performed_by_display,
                 a.authorized_by,
                 a.occurred_at_utc,
                 a.external_effect_name,
                 a.external_effect_applied,
                 a.external_effect_error,
                 COUNT(1) OVER()                          AS total_count
        """;

    private const string DenialColumns = """
                 d.public_id,
                 d.permission_code,
                 d.route,
                 d.http_method,
                 d.attempted_by,
                 d.attempted_by_display,
                 d.enforcement_enabled,
                 d.blocked,
                 d.occurred_at_utc,
                 COUNT(1) OVER()                          AS total_count
        """;

    /// <summary>
    /// Pagina de la bitacora de acciones.
    ///
    /// El ORDER BY es UNICO: la fecha ordena y el id desempata. Sin desempate, dos renglones del
    /// mismo instante -y en una bitacora los hay- pueden salir dos veces en una pagina y no salir
    /// en la siguiente, sin error y sin sintoma.
    /// </summary>
    public static string ActionsPage(ActionAuditFilterShape shape) => $"""
        SELECT
        {ActionColumns}
        FROM     action_audit_entry a
        WHERE    {TenantScope.On("a")}
        {ActionFilters(shape)}
        ORDER BY a.occurred_at_utc DESC, a.id DESC
        OFFSET   @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
        """;

    public static string DenialsPage(AccessDenialFilterShape shape) => $"""
        SELECT
        {DenialColumns}
        FROM     access_denial_entry d
        WHERE    {TenantScope.On("d")}
        {DenialFilters(shape)}
        ORDER BY d.occurred_at_utc DESC, d.id DESC
        OFFSET   @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
        """;

    /// <summary>
    /// Los filtros, uno por linea y siempre en la misma forma. Cada uno es un fragmento FIJO:
    /// lo unico que decide la forma de los filtros es si el fragmento entra o no.
    /// </summary>
    private static string ActionFilters(ActionAuditFilterShape shape)
    {
        var clauses = new List<string>();

        // El rango entra como comparacion sobre la columna, no como funcion aplicada a ella:
        // date_trunc(...) = @Dia dejaria el indice por fecha inservible.
        if (shape.HasFrom) clauses.Add("a.occurred_at_utc >= @FromUtc");
        if (shape.HasTo) clauses.Add("a.occurred_at_utc <  @ToUtc");
        if (shape.HasActionCode) clauses.Add("a.action_code = @ActionCode");
        if (shape.HasSubjectKey) clauses.Add("a.subject_key = @SubjectKey");
        if (shape.HasPerformedBy) clauses.Add("a.performed_by ILIKE '%' || @PerformedBy || '%'");

        // Los pendientes de efecto externo. Es el filtro por el que mas gente abre esta pantalla,
        // y tiene su propio indice filtrado detras.
        if (shape.OnlyPendingExternalEffect) clauses.Add("a.external_effect_applied = false");

        return Compose(clauses);
    }

    private static string DenialFilters(AccessDenialFilterShape shape)
    {
        var clauses = new List<string>();

        if (shape.HasFrom) clauses.Add("d.occurred_at_utc >= @FromUtc");
        if (shape.HasTo) clauses.Add("d.occurred_at_utc <  @ToUtc");
        if (shape.HasPermissionCode) clauses.Add("d.permission_code = @PermissionCode");
        if (shape.HasAttemptedBy) clauses.Add("d.attempted_by ILIKE '%' || @AttemptedBy || '%'");

        // "Que se habria bloqueado si el flag estuviera encendido": la lista que se revisa antes
        // de encenderlo.
        if (shape.OnlyWouldHaveBeenBlocked) clauses.Add("d.blocked = false");

        return Compose(clauses);
    }

    private static string Compose(IReadOnlyList<string> clauses) =>
        clauses.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, clauses.Select(clause => $"  AND    {clause}"));

    /// <summary>
    /// TODAS las combinaciones de filtros de la bitacora de acciones. No es codigo de produccion
    /// disfrazado: existe para que la prueba de aislamiento recorra cada variante del SQL, porque
    /// un filtro que cambia la consulta puede cambiar tambien lo que la consulta olvida.
    /// </summary>
    public static IReadOnlyList<ActionAuditFilterShape> AllActionShapes { get; } =
        Combinations(6)
            .Select(bits => new ActionAuditFilterShape(bits[0], bits[1], bits[2], bits[3], bits[4], bits[5]))
            .ToList();

    public static IReadOnlyList<AccessDenialFilterShape> AllDenialShapes { get; } =
        Combinations(5)
            .Select(bits => new AccessDenialFilterShape(bits[0], bits[1], bits[2], bits[3], bits[4]))
            .ToList();

    private static IEnumerable<bool[]> Combinations(int length)
    {
        var total = 1 << length;
        for (var mask = 0; mask < total; mask++)
        {
            var bits = new bool[length];
            for (var bit = 0; bit < length; bit++)
                bits[bit] = (mask & (1 << bit)) != 0;
            yield return bits;
        }
    }
}
