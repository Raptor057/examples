using Audit.Domain.Entities;
using Audit.Domain.Repositories;
using SaasAccessAudit.Shared.Persistence;

namespace Audit.Infrastructure.Repositories;

/// <summary>
/// Las altas de la bitacora, por EF Core. Solo altas: no hay update ni delete, y la tabla ademas
/// los rechaza en el motor con un disparador.
///
/// El tenant lo sella el interceptor al insertar, desde el contexto de la peticion. Por eso
/// ningun renglon de bitacora puede quedar con el tenant equivocado ni sin tenant.
/// </summary>
internal sealed class AuditWriteRepository(AppDbContext db) : IAuditWriteRepository
{
    public async Task AppendAsync(ActionAuditEntry entry, CancellationToken cancellationToken = default)
    {
        db.Set<ActionAuditEntry>().Add(entry);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AppendAsync(AccessDenialEntry entry, CancellationToken cancellationToken = default)
    {
        db.Set<AccessDenialEntry>().Add(entry);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
