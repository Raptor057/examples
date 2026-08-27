using Audit.Contracts;
using Audit.Domain.Entities;
using Audit.Domain.Repositories;
using Microsoft.Extensions.Logging;
using SaasAccessAudit.Kernel;

namespace Audit.Infrastructure.Repositories;

/// <summary>
/// La bitacora de decisiones de acceso. Mismo criterio que la de acciones: el "quien" sale del
/// token y un fallo escribiendo no puede convertir un 403 en un 500.
/// </summary>
internal sealed class AccessDecisionLog(
    IAuditWriteRepository repository,
    IUserContextAccessor userAccessor,
    ILogger<AccessDecisionLog> logger) : IAccessDecisionLog
{
    public async Task RecordDenialAsync(AccessDenialRecord record, CancellationToken cancellationToken = default)
    {
        var user = userAccessor.Current;

        var entry = new AccessDenialEntry
        {
            PermissionCode = record.PermissionCode,
            Route = record.Route,
            HttpMethod = record.HttpMethod,
            AttemptedBy = user?.Username ?? "anonimo",
            AttemptedByDisplay = user?.DisplayName ?? "anonimo",
            EnforcementEnabled = record.EnforcementEnabled,
            Blocked = record.Blocked
        };

        try
        {
            await repository.AppendAsync(entry, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "No fue posible registrar el rechazo de {PermissionCode} en {Route} para {AttemptedBy}.",
                record.PermissionCode, record.Route, entry.AttemptedBy);
        }
    }
}
