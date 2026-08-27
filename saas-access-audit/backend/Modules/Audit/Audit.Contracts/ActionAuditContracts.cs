namespace Audit.Contracts;

/// <summary>
/// Lo que un modulo entrega para que quede constancia de una accion sensible.
///
/// Fijate en lo que NO trae: no trae quien la hizo ni cuando. El "quien" lo pone la
/// implementacion desde el contexto autenticado -si viniera aqui, cualquiera podria firmar la
/// accion con el nombre de otro- y el "cuando" lo pone el motor de base de datos.
/// </summary>
/// <param name="ActionCode">
/// El MISMO codigo del permiso que autorizo la accion. Ese detalle es el que permite cruzar las
/// dos bitacoras: para una accion cualquiera se puede ver quien la ejecuto y quien lo intento
/// sin poder, buscando por el mismo codigo en las dos pantallas.
/// </param>
/// <param name="SubjectType">Familia de lo afectado, para poder filtrar sin adivinar.</param>
/// <param name="SubjectKey">
/// LA LLAVE de lo afectado, no su descripcion. Las descripciones cambian, y el dia que alguien
/// renombre al usuario el renglon de bitacora dejaria de encontrarse.
/// </param>
/// <param name="SubjectLabel">
/// Contexto minimo para leer el renglon sin unirse a nada mas. Es una FOTO del momento: si
/// despues cambia, el renglon sigue diciendo como se llamaba cuando pasó.
/// </param>
/// <param name="Reason">Motivo escrito por la persona. Obligatorio, con CHECK en la base.</param>
/// <param name="AuthorizedBy">Solo si hubo autorizacion por excepcion. Normalmente null.</param>
/// <param name="ExternalEffectName">Nombre del efecto externo, si la accion tuvo uno.</param>
/// <param name="ExternalEffectApplied">Si ese efecto quedo aplicado. null si no aplicaba.</param>
/// <param name="ExternalEffectError">El error, cuando no quedo aplicado.</param>
public sealed record ActionAuditRecord(
    string ActionCode,
    string SubjectType,
    string SubjectKey,
    string SubjectLabel,
    string Reason,
    string? AuthorizedBy = null,
    string? ExternalEffectName = null,
    bool? ExternalEffectApplied = null,
    string? ExternalEffectError = null);

/// <summary>
/// La bitacora de acciones, vista desde los modulos que la alimentan.
///
/// Es la unica via por la que otro modulo escribe en ella, y vive en Contracts porque Contracts
/// es el unico canal legitimo entre modulos.
/// </summary>
public interface IActionAuditLog
{
    /// <summary>
    /// Registra la accion. NO LANZA: la accion ya ocurrio y fallar aqui no la deshace, asi que
    /// tumbar la peticion solo lograria que el usuario la repitiera. Lo que si hace es dejar un
    /// error en el log de la aplicacion; un catch vacio deja la accion sin rastro y sin nadie
    /// enterado, que es la peor combinacion posible.
    /// </summary>
    Task RecordAsync(ActionAuditRecord record, CancellationToken cancellationToken = default);
}
