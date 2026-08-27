namespace Audit.Contracts;

/// <summary>Un intento que no paso la politica del endpoint, o que no la habria pasado.</summary>
/// <param name="PermissionCode">El permiso que faltaba.</param>
/// <param name="Route">Ruta pedida. Sirve para encontrar el endpoint exacto al diagnosticar.</param>
/// <param name="HttpMethod">Metodo HTTP de la peticion.</param>
/// <param name="EnforcementEnabled">Estado del flag al decidir.</param>
/// <param name="Blocked">
/// Si la peticion se detuvo de verdad. En modo auditoria vale false: el renglon dice
/// "esto se habria bloqueado", que es justo la lista que alguien revisa antes de encender el flag.
/// </param>
public sealed record AccessDenialRecord(
    string PermissionCode,
    string Route,
    string HttpMethod,
    bool EnforcementEnabled,
    bool Blocked);

/// <summary>
/// La bitacora de decisiones de acceso.
///
/// POR QUE ES OTRA TABLA Y NO LA MISMA QUE LAS ACCIONES. La skill obliga a contestar dos
/// preguntas antes de reusar una bitacora existente, y aqui las dos dan que no:
///
///   1. Campos obligatorios. La bitacora de acciones exige MOTIVO, con CHECK de no vacio en la
///      base. Un intento bloqueado no tiene motivo: la persona nunca llego a escribirlo, porque
///      el filtro corto la peticion antes del formulario. Meterlo ahi obligaria a inventar un
///      motivo, y un motivo inventado envenena la unica columna que la bitacora existe para
///      responder.
///   2. Acciones disponibles. La pantalla de acciones ofrece ver el efecto externo y su
///      pendiente. Un rechazo no tiene efectos: no hizo nada.
///
/// Ademas responden preguntas distintas. La de acciones contesta "que se hizo y por que"; esta
/// contesta "a quien le falta un permiso", que es una pregunta de configuracion, no de negocio.
/// </summary>
public interface IAccessDecisionLog
{
    /// <summary>
    /// Registra el rechazo. Igual que la de acciones, no lanza: un fallo escribiendo la bitacora
    /// no puede convertir un 403 en un 500.
    /// </summary>
    Task RecordDenialAsync(AccessDenialRecord record, CancellationToken cancellationToken = default);
}
