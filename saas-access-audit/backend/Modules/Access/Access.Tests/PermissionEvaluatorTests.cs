using Access.Contracts;
using Access.Infrastructure.Authorization;
using SaasAccessAudit.Kernel;

namespace Access.Tests;

/// <summary>
/// LA BANDERA DE ENFORCEMENT Y EL RASTRO DE LO NEGADO, QUE SON LA MISMA PIEZA.
///
/// Las tres situaciones que el ejemplo existe para ensenar caben en este archivo:
///   - quien pudo   -> pasa, y no ensucia la bitacora de accesos,
///   - quien no pudo con el flag ENCENDIDO -> se corta, y queda registrado como bloqueado,
///   - quien no pudo con el flag APAGADO   -> pasa igual, y queda registrado como
///     "se habria bloqueado", que es la lista de trabajo antes de encenderlo.
/// </summary>
public sealed class PermissionEvaluatorTests
{
    private static readonly UserContext Viewer =
        new("dario.luna", "Dario Luna", ["saas-viewers"]);

    private static (PermissionEvaluator Evaluator, FakeAccessDecisionLog Log) Build(
        IReadOnlyList<string> permissions, bool enforce, UserContext? user = null)
    {
        var repository = new FakeAccessReadRepository { Permissions = [.. permissions] };
        var log = new FakeAccessDecisionLog();
        var evaluator = new PermissionEvaluator(
            new FakeUserContextAccessor(user ?? Viewer),
            repository,
            new FakeAccessControlSettings(enforce),
            log);

        return (evaluator, log);
    }

    [Fact]
    public async Task Quien_tiene_el_permiso_pasa_y_no_deja_rastro_de_rechazo()
    {
        var (evaluator, log) = Build([PermissionCatalog.UsersDeactivate], enforce: true);

        var decision = await evaluator.EvaluateAsync(
            PermissionCatalog.UsersDeactivate,
            "/api/access/users/x/deactivate",
            "POST",
            TestContext.Current.CancellationToken);

        Assert.True(decision.Allowed);
        Assert.True(decision.HasPermission);
        Assert.False(decision.WouldHaveBeenBlocked);

        // La bitacora de ACCESOS registra rechazos, no exitos. Lo que hizo esa persona queda en
        // la de ACCIONES, y solo si la accion era sensible.
        Assert.Empty(log.Records);
    }

    [Fact]
    public async Task Con_el_flag_encendido_quien_no_lo_tiene_se_bloquea_y_queda_registrado()
    {
        var (evaluator, log) = Build([PermissionCatalog.UsersView], enforce: true);

        var decision = await evaluator.EvaluateAsync(
            PermissionCatalog.UsersDeactivate,
            "/api/access/users/x/deactivate",
            "POST",
            TestContext.Current.CancellationToken);

        Assert.False(decision.Allowed);
        Assert.False(decision.HasPermission);

        var record = Assert.Single(log.Records);
        Assert.Equal(PermissionCatalog.UsersDeactivate, record.PermissionCode);
        Assert.Equal("/api/access/users/x/deactivate", record.Route);
        Assert.Equal("POST", record.HttpMethod);
        Assert.True(record.EnforcementEnabled);
        Assert.True(record.Blocked);
    }

    [Fact]
    public async Task Con_el_flag_apagado_la_peticion_pasa_y_queda_como_se_habria_bloqueado()
    {
        var (evaluator, log) = Build([PermissionCatalog.UsersView], enforce: false);

        var decision = await evaluator.EvaluateAsync(
            PermissionCatalog.UsersDeactivate,
            "/api/access/users/x/deactivate",
            "POST",
            TestContext.Current.CancellationToken);

        // Eso es el modo auditoria: no bloquea, pero deja escrito lo que habria bloqueado.
        Assert.True(decision.Allowed);
        Assert.False(decision.HasPermission);
        Assert.True(decision.WouldHaveBeenBlocked);

        var record = Assert.Single(log.Records);
        Assert.False(record.EnforcementEnabled);
        Assert.False(record.Blocked);
    }

    [Fact]
    public async Task Sin_identidad_no_hay_permisos()
    {
        // Fail-closed. Una peticion sin token que llega a un endpoint con politica no pasa
        // "porque no se pudo comprobar".
        var repository = new FakeAccessReadRepository { Permissions = [PermissionCatalog.UsersDeactivate] };
        var evaluator = new PermissionEvaluator(
            new FakeUserContextAccessor(null),
            repository,
            new FakeAccessControlSettings(true),
            new FakeAccessDecisionLog());

        var decision = await evaluator.EvaluateAsync(PermissionCatalog.UsersDeactivate, "/x", "POST", TestContext.Current.CancellationToken);

        Assert.False(decision.Allowed);
    }

    [Fact]
    public async Task Los_permisos_se_calculan_una_vez_por_peticion()
    {
        // El cache es de la PETICION, no entre peticiones: quitarle un rol a alguien tiene que
        // surtir efecto en la siguiente llamada, no cuando caduque su sesion.
        var repository = new FakeAccessReadRepository { Permissions = [PermissionCatalog.UsersView] };
        var evaluator = new PermissionEvaluator(
            new FakeUserContextAccessor(Viewer),
            repository,
            new FakeAccessControlSettings(true),
            new FakeAccessDecisionLog());

        await evaluator.EvaluateAsync(PermissionCatalog.UsersView, "/a", "GET", TestContext.Current.CancellationToken);
        await evaluator.EvaluateAsync(PermissionCatalog.RolesView, "/b", "GET", TestContext.Current.CancellationToken);
        await evaluator.GetEffectivePermissionsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, repository.EffectivePermissionCalls);
    }

    [Fact]
    public async Task Un_typo_en_el_codigo_se_comporta_como_permiso_faltante()
    {
        // Es exactamente por esto que la politica se declara con la constante del catalogo y hay
        // una prueba que recorre los endpoints: el sintoma de un typo es indistinguible.
        var (evaluator, log) = Build([PermissionCatalog.UsersDeactivate], enforce: true);

        var decision = await evaluator.EvaluateAsync("access:user:deactivat", "/api/x", "POST", TestContext.Current.CancellationToken);

        Assert.False(decision.Allowed);
        Assert.Single(log.Records);
    }
}
