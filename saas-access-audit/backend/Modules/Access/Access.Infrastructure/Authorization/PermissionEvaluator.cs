using Access.Domain.Repositories;
using Audit.Contracts;
using SaasAccessAudit.Kernel;

namespace Access.Infrastructure.Authorization;

/// <summary>
/// DONDE SE JUNTAN LOS DOS PATRONES.
///
/// Es el unico punto del sistema que contesta "puede o no puede", y por eso es tambien el unico
/// que puede garantizar que TODO rechazo queda registrado. Si el registro lo hiciera cada
/// endpoint, el primero que se olvidara dejaria un hueco que nadie nota: la pantalla se veria
/// igual y la bitacora estaria incompleta sin dar sintoma.
///
/// El servicio es SCOPED y cachea los permisos efectivos durante la peticion. Ese es todo el
/// alcance del cache: entre peticiones no se guarda nada, para que quitarle un rol a alguien
/// surta efecto en la siguiente llamada y no cuando caduque su sesion.
/// </summary>
internal sealed class PermissionEvaluator(
    IUserContextAccessor userAccessor,
    IAccessReadRepository repository,
    IAccessControlSettings settings,
    IAccessDecisionLog decisionLog) : IPermissionEvaluator
{
    private IReadOnlyList<string>? _cachedPermissions;

    public async Task<AccessDecision> EvaluateAsync(
        string permissionCode,
        string route,
        string httpMethod,
        CancellationToken cancellationToken = default)
    {
        // El flag se lee AQUI, en cada decision, y no se guarda al arrancar: cambiarlo en la
        // configuracion surte efecto sin reiniciar, que es lo que hace practico encenderlo y
        // mirar que pasa.
        var enforcementEnabled = settings.EnforcePermissions;

        var permissions = await GetEffectivePermissionsAsync(cancellationToken).ConfigureAwait(false);
        var hasPermission = permissions.Contains(permissionCode, StringComparer.Ordinal);

        // Con el flag apagado la peticion pasa igual: eso es el modo auditoria.
        var allowed = hasPermission || !enforcementEnabled;

        if (hasPermission)
            return new AccessDecision(true, true, enforcementEnabled);

        // No lo tenia. Queda constancia en los dos modos, y la columna Blocked es la que
        // distingue "se bloqueo" de "se habria bloqueado".
        await decisionLog.RecordDenialAsync(
            new AccessDenialRecord(
                PermissionCode: permissionCode,
                Route: route,
                HttpMethod: httpMethod,
                EnforcementEnabled: enforcementEnabled,
                Blocked: !allowed),
            cancellationToken).ConfigureAwait(false);

        return new AccessDecision(allowed, false, enforcementEnabled);
    }

    public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedPermissions is not null) return _cachedPermissions;

        // Sin identidad no hay permisos. Fail-closed: un endpoint con politica que llega sin
        // token no pasa, en vez de pasar "porque no se pudo comprobar".
        var groups = userAccessor.Current?.Groups ?? [];

        _cachedPermissions = await repository
            .GetEffectivePermissionCodesAsync(groups, cancellationToken)
            .ConfigureAwait(false);

        return _cachedPermissions;
    }
}
