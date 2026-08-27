using Audit.Domain.Models;
using SaasAccessAudit.Kernel;

namespace Audit.Domain.Repositories;

/// <summary>
/// Las lecturas pesadas de la bitacora. UNICO lugar del proyecto que va por Dapper.
///
/// El motivo esta escrito en TenantScope y en el README: la pantalla necesita una pagina y el
/// total del conjunto filtrado en la MISMA pasada (COUNT(1) OVER()), con seis filtros opcionales
/// que cambian el predicado. EF Core lo expresa, pero componiendo la consulta con IQueryable el
/// SQL final deja de estar a la vista, y en una tabla que crece sin parar eso es justo lo que no
/// se quiere perder.
///
/// El precio de bajar a SQL crudo en un proyecto con tenancy se paga en TenantScope y en la
/// prueba que recorre todas las variantes de estas consultas.
/// </summary>
public interface IAuditReadRepository
{
    Task<PagedResult<ActionAuditItem>> GetActionsPageAsync(
        ActionAuditFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<PagedResult<AccessDenialItem>> GetDenialsPageAsync(
        AccessDenialFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
