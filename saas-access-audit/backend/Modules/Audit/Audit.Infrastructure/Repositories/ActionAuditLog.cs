using Audit.Contracts;
using Audit.Domain.Entities;
using Audit.Domain.Repositories;
using Microsoft.Extensions.Logging;
using SaasAccessAudit.Kernel;

namespace Audit.Infrastructure.Repositories;

/// <summary>
/// La bitacora de acciones, vista desde los modulos que la alimentan.
///
/// Dos cosas pasan aqui y las dos importan:
///
/// 1. EL "QUIEN" SE PONE AQUI, desde el contexto autenticado. El record que llega del otro modulo
///    no lo trae y no puede traerlo: si el usuario viajara en el cuerpo de la peticion,
///    cualquiera podria firmar una accion con el nombre de otro y la bitacora acusaria a la
///    persona equivocada.
///
/// 2. ESCRIBIR LA BITACORA NO PUEDE TUMBAR LA ACCION. La accion YA OCURRIO; fallar aqui no la
///    deshace, y devolver un error solo lograria que el usuario la repitiera. Por eso va en
///    try/catch. Pero el catch NO ESTA VACIO: deja un error en el log de la aplicacion. Un catch
///    vacio deja la accion sin rastro y sin nadie enterado, que es la peor combinacion posible.
/// </summary>
internal sealed class ActionAuditLog(
    IAuditWriteRepository repository,
    IUserContextAccessor userAccessor,
    ILogger<ActionAuditLog> logger) : IActionAuditLog
{
    public async Task RecordAsync(ActionAuditRecord record, CancellationToken cancellationToken = default)
    {
        var user = userAccessor.Current;

        var entry = new ActionAuditEntry
        {
            ActionCode = record.ActionCode,
            SubjectType = record.SubjectType,
            SubjectKey = record.SubjectKey,
            SubjectLabel = record.SubjectLabel,
            Reason = record.Reason,
            PerformedBy = user?.Username ?? "desconocido",
            PerformedByDisplay = user?.DisplayName ?? "desconocido",
            AuthorizedBy = record.AuthorizedBy,
            ExternalEffectName = record.ExternalEffectName,
            ExternalEffectApplied = record.ExternalEffectApplied,
            ExternalEffectError = record.ExternalEffectError
        };

        // OccurredAtUtc se queda sin asignar a proposito: la fecha la pone el motor con su
        // default (now()), y asi dos replicas con relojes distintos no producen una linea de
        // tiempo que se contradice a si misma.

        try
        {
            await repository.AppendAsync(entry, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "No fue posible registrar en la bitacora la accion {ActionCode} sobre {SubjectKey} hecha por {PerformedBy}. La accion SI se ejecuto.",
                record.ActionCode, record.SubjectKey, entry.PerformedBy);
        }
    }
}
