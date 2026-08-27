namespace SaasAccessAudit.Kernel;

/// <summary>
/// Veredicto de una politica de permiso sobre la peticion en curso.
/// </summary>
/// <param name="Allowed">Si la peticion continua. Con enforcement apagado es siempre true.</param>
/// <param name="HasPermission">Si la persona TIENE el permiso de verdad, al margen del flag.</param>
/// <param name="EnforcementEnabled">Estado del flag en el momento de decidir.</param>
public sealed record AccessDecision(bool Allowed, bool HasPermission, bool EnforcementEnabled)
{
    /// <summary>
    /// Modo auditoria: no tenia el permiso, pero el flag estaba apagado y la peticion siguio.
    /// Es exactamente el renglon que alguien quiere ver antes de encender el flag.
    /// </summary>
    public bool WouldHaveBeenBlocked => !HasPermission && !EnforcementEnabled;
}

/// <summary>
/// El puente entre la capa web y el modulo que sabe de roles.
///
/// Vive en el Kernel (sin dependencias) para que el filtro de autorizacion de la capa web
/// pueda exigir un permiso sin conocer el modulo Access, y para que el modulo Access pueda
/// implementarlo sin conocer la capa web. Cambiar el origen de los permisos -otra base, un
/// servicio externo- es cambiar esta implementacion y nada mas.
/// </summary>
public interface IPermissionEvaluator
{
    /// <summary>
    /// Decide, y ademas DEJA RASTRO de lo que negó (o de lo que habria negado con el flag
    /// encendido). El registro va aqui adentro a proposito: si lo hiciera cada endpoint, el
    /// primero que se olvidara dejaria un hueco invisible en la bitacora.
    /// </summary>
    Task<AccessDecision> EvaluateAsync(
        string permissionCode,
        string route,
        string httpMethod,
        CancellationToken cancellationToken = default);

    /// <summary>Permisos efectivos de la persona actual. Los usa la pantalla para ocultar.</summary>
    Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(CancellationToken cancellationToken = default);
}
