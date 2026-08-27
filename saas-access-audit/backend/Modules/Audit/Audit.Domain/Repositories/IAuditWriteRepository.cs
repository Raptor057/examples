using Audit.Domain.Entities;

namespace Audit.Domain.Repositories;

/// <summary>
/// Las escrituras de la bitacora, por EF Core como todas las del proyecto.
///
/// Solo hay altas. No existe un UpdateAsync ni un DeleteAsync, y no es que falten: no pueden
/// existir. La tabla ademas los rechaza en el motor.
/// </summary>
public interface IAuditWriteRepository
{
    Task AppendAsync(ActionAuditEntry entry, CancellationToken cancellationToken = default);

    Task AppendAsync(AccessDenialEntry entry, CancellationToken cancellationToken = default);
}
