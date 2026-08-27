using Audit.Domain.Models;
using Audit.Infrastructure.Persistence.Sql.MainDb;

namespace Audit.Tests;

/// <summary>
/// La forma del SQL de la bitacora. Lo que se comprueba aqui no es que devuelva datos -eso lo
/// dice la base- sino que cumpla las reglas que hacen que la pantalla sea correcta: paginado real
/// en el servidor, orden unico, total del conjunto filtrado y valores parametrizados.
/// </summary>
public sealed class AuditSqlShapeTests
{
    public static TheoryData<string, string> AllStatements() => TenantIsolationSqlTests.AllStatements();

    [Theory]
    [MemberData(nameof(AllStatements))]
    public void Se_pagina_con_offset_fetch_y_nunca_con_top(string name, string sql)
    {
        Assert.True(
            sql.Contains("OFFSET   @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", StringComparison.Ordinal),
            $"La consulta '{name}' no pagina con OFFSET/FETCH.");

        // TOP no pagina: recorta. Con TOP no hay pagina dos, y el usuario no puede saber que hay
        // mas renglones de los que ve.
        Assert.True(
            !sql.Contains(" TOP ", StringComparison.OrdinalIgnoreCase),
            $"La consulta '{name}' usa TOP en vez de paginar.");
    }

    [Theory]
    [MemberData(nameof(AllStatements))]
    public void El_total_del_conjunto_filtrado_viaja_con_la_pagina(string name, string sql) =>
        // En la MISMA pasada, con el MISMO predicado. Calcularlo en una segunda consulta con el
        // predicado copiado es como se llega a que el contador no cuadre con la lista estando las
        // dos bien por separado.
        Assert.True(
            sql.Contains("COUNT(1) OVER()", StringComparison.Ordinal),
            $"La consulta '{name}' no devuelve el total del conjunto filtrado.");

    [Theory]
    [MemberData(nameof(AllStatements))]
    public void El_orden_es_unico(string name, string sql)
    {
        // La fecha ordena y el id desempata. Sin desempate, dos renglones del mismo instante
        // -y en una bitacora los hay- pueden salir dos veces en una pagina y faltar en la
        // siguiente, sin error y sin sintoma.
        var hasUniqueOrder =
            sql.Contains("ORDER BY a.occurred_at_utc DESC, a.id DESC", StringComparison.Ordinal)
            || sql.Contains("ORDER BY d.occurred_at_utc DESC, d.id DESC", StringComparison.Ordinal);

        Assert.True(hasUniqueOrder, $"La consulta '{name}' no ordena por una llave unica.");
    }

    [Theory]
    [MemberData(nameof(AllStatements))]
    public void Lo_mas_reciente_va_primero(string name, string sql) =>
        Assert.True(
            sql.Contains("occurred_at_utc DESC", StringComparison.Ordinal),
            $"La consulta '{name}' no ordena de lo mas reciente a lo mas viejo.");

    [Fact]
    public void Cada_filtro_activo_agrega_su_fragmento_y_ninguno_interpola_valores()
    {
        var sinFiltros = AuditSql.ActionsPage(new ActionAuditFilterShape(false, false, false, false, false, false));
        var conTodo = AuditSql.ActionsPage(new ActionAuditFilterShape(true, true, true, true, true, true));

        Assert.DoesNotContain("@FromUtc", sinFiltros, StringComparison.Ordinal);
        Assert.DoesNotContain("@ActionCode", sinFiltros, StringComparison.Ordinal);

        // El tipo de filtro elige un fragmento FIJO; el valor viaja parametrizado siempre. Por eso
        // estos metodos ni siquiera reciben los valores: interpolarlos seria imposible.
        Assert.Contains("@FromUtc", conTodo, StringComparison.Ordinal);
        Assert.Contains("@ToUtc", conTodo, StringComparison.Ordinal);
        Assert.Contains("@ActionCode", conTodo, StringComparison.Ordinal);
        Assert.Contains("@SubjectKey", conTodo, StringComparison.Ordinal);
        Assert.Contains("@PerformedBy", conTodo, StringComparison.Ordinal);
    }

    [Fact]
    public void El_rango_de_fechas_compara_la_columna_y_no_una_funcion_sobre_ella()
    {
        // date_trunc('day', columna) = @Dia dejaria el indice por fecha inservible y convertiria
        // el filtro en un barrido de la bitacora entera.
        var conRango = AuditSql.ActionsPage(new ActionAuditFilterShape(true, true, false, false, false, false));

        Assert.Contains("a.occurred_at_utc >= @FromUtc", conRango, StringComparison.Ordinal);
        Assert.Contains("a.occurred_at_utc <  @ToUtc", conRango, StringComparison.Ordinal);
        Assert.DoesNotContain("date_trunc", conRango, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("date_part", conRango, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void El_filtro_de_pendientes_mira_solo_los_false_y_no_los_nulos()
    {
        // null significa "esta accion no tenia efecto externo"; false significa "lo tenia y quedo
        // pendiente". Confundirlos llenaria la pantalla de pendientes de renglones que nunca
        // tuvieron nada que aplicar.
        var pendientes = AuditSql.ActionsPage(new ActionAuditFilterShape(false, false, false, false, false, true));

        Assert.Contains("a.external_effect_applied = false", pendientes, StringComparison.Ordinal);
        Assert.DoesNotContain("IS NULL", pendientes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void La_consulta_de_accesos_puede_pedir_solo_lo_que_se_habria_bloqueado()
    {
        var lista = AuditSql.DenialsPage(new AccessDenialFilterShape(false, false, false, false, true));

        Assert.Contains("d.blocked = false", lista, StringComparison.Ordinal);
    }

    [Fact]
    public void La_bitacora_solo_se_lee()
    {
        // No existe SQL de UPDATE ni de DELETE en esta clase, y no es que falte. Ademas la base
        // los rechaza con un disparador.
        foreach (var shape in AuditSql.AllActionShapes)
        {
            var sql = AuditSql.ActionsPage(shape);
            Assert.DoesNotContain("UPDATE ", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE ", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("INSERT ", sql, StringComparison.OrdinalIgnoreCase);
        }
    }
}
