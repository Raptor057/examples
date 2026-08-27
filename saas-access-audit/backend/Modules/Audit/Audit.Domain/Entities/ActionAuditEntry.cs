using SaasAccessAudit.Kernel;

namespace Audit.Domain.Entities;

/// <summary>
/// LA BITACORA DE ACCIONES SENSIBLES.
///
/// Es APPEND-ONLY: se inserta y nunca se edita ni se borra. No declara ISoftDeletable ni
/// IAuditable, y eso es deliberado -no un descuido del chasis-: un renglon con IsActive se puede
/// apagar, y una bitacora que se puede apagar no es una bitacora. Tampoco lleva UpdatedAtUtc,
/// porque no hay actualizacion que fechar. Ademas de la ausencia de columnas, la migracion pone
/// un DISPARADOR que hace fallar cualquier UPDATE o DELETE sobre la tabla: la promesa la sostiene
/// el motor, no la buena voluntad de quien escriba el proximo repositorio.
///
/// SI declara ITenantOwned. Una bitacora sin la llave de aislamiento es una fuga entre tenants,
/// y de las peores: quien la lea vera lo que hizo la competencia.
///
/// NO ES UN RESPALDO. Guarda QUE se hizo y QUIEN lo hizo, no el contenido de lo que se cambio.
/// Reponer sigue siendo trabajo manual, y conviene saberlo antes de necesitarlo.
/// </summary>
public sealed class ActionAuditEntry : BaseEntity, ITenantOwned
{
    public long TenantId { get; set; }

    /// <summary>Codigo del permiso que autorizo la accion. Es la llave del cruce entre las dos bitacoras.</summary>
    public string ActionCode { get; set; } = string.Empty;

    public string SubjectType { get; set; } = string.Empty;

    /// <summary>La LLAVE de lo afectado. Nunca su descripcion.</summary>
    public string SubjectKey { get; set; } = string.Empty;

    /// <summary>Contexto minimo: como se llamaba en el momento de la accion.</summary>
    public string SubjectLabel { get; set; } = string.Empty;

    /// <summary>Motivo escrito por la persona. La base lo defiende con un CHECK de no vacio.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Quien actuo. Sale del token, nunca del cuerpo de la peticion.</summary>
    public string PerformedBy { get; set; } = string.Empty;

    public string PerformedByDisplay { get; set; } = string.Empty;

    /// <summary>Quien autorizo por excepcion. Null cuando la accion no necesito autorizacion extra.</summary>
    public string? AuthorizedBy { get; set; }

    /// <summary>
    /// Cuando. La pone el MOTOR por defecto (now()), no el proceso: dos servidores con el reloj
    /// desfasado producirian una linea de tiempo que no cuadra consigo misma.
    /// </summary>
    public DateTime OccurredAtUtc { get; set; }

    public string? ExternalEffectName { get; set; }

    /// <summary>
    /// Si el efecto externo quedo aplicado. Null cuando la accion no tenia efecto externo, para
    /// que "no aplicaba" y "quedo pendiente" no se confundan en la pantalla ni en el indice.
    /// </summary>
    public bool? ExternalEffectApplied { get; set; }

    public string? ExternalEffectError { get; set; }
}
