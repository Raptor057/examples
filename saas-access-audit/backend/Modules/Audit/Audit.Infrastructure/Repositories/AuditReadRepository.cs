using Audit.Domain.Models;
using Audit.Domain.Repositories;
using Audit.Infrastructure.Persistence.DbModels.MainDb;
using Audit.Infrastructure.Persistence.Sql.MainDb;
using SaasAccessAudit.Kernel;
using SaasAccessAudit.Shared.Postgres;
using SaasAccessAudit.Shared.Postgres.Markers;
using SaasAccessAudit.Shared.Tenancy;

namespace Audit.Infrastructure.Repositories;

/// <summary>
/// El UNICO repositorio del proyecto que va por Dapper.
///
/// El repositorio solo EJECUTA el SQL de su clase AuditSql y mapea la fila al modelo de dominio.
/// No arma consultas, no abre conexiones y no compone el filtro de tenant: eso ultimo lo hace
/// TenantScope.Bind, que toma el valor del contexto de la peticion. En este archivo no aparece un
/// solo literal de tenant, y esa es exactamente la garantia que hace aceptable la excepcion.
/// </summary>
internal sealed class AuditReadRepository(
    ConfigurationSqlDbConnection<MainDb> connection,
    TenantScope tenantScope) : IAuditReadRepository
{
    public async Task<PagedResult<ActionAuditItem>> GetActionsPageAsync(
        ActionAuditFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var sql = AuditSql.ActionsPage(filter.Shape);

        // Bind pone el tenant. Los demas parametros son de PRESENTACION y si vienen del cliente,
        // ya normalizados por el handler.
        var parameters = tenantScope.Bind(new
        {
            FromUtc = filter.FromUtc,
            ToUtc = filter.ToUtc,
            ActionCode = filter.ActionCode,
            SubjectKey = filter.SubjectKey,
            PerformedBy = filter.PerformedBy,
            Offset = Paging.Offset(pageNumber, pageSize),
            PageSize = pageSize
        });

        var rows = (await connection
            .QueryAsync<ActionAuditRow>(sql, parameters, queryName: "AuditSql.ActionsPage", cancellationToken: cancellationToken)
            .ConfigureAwait(false)).ToList();

        var items = rows
            .Select(row => new ActionAuditItem(
                row.PublicId,
                row.ActionCode,
                row.SubjectType,
                row.SubjectKey,
                row.SubjectLabel,
                row.Reason,
                row.PerformedBy,
                row.PerformedByDisplay,
                row.AuthorizedBy,
                row.OccurredAtUtc,
                row.ExternalEffectName,
                row.ExternalEffectApplied,
                row.ExternalEffectError))
            .ToList();

        // El total lo trae cada fila (COUNT(1) OVER()). Con la pagina vacia no hay filas de donde
        // leerlo, y entonces el total honesto es 0: no hay nada que contar en ese filtro.
        var totalCount = rows.Count == 0 ? 0 : rows[0].TotalCount;

        return new PagedResult<ActionAuditItem>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<AccessDenialItem>> GetDenialsPageAsync(
        AccessDenialFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var sql = AuditSql.DenialsPage(filter.Shape);

        var parameters = tenantScope.Bind(new
        {
            FromUtc = filter.FromUtc,
            ToUtc = filter.ToUtc,
            PermissionCode = filter.PermissionCode,
            AttemptedBy = filter.AttemptedBy,
            Offset = Paging.Offset(pageNumber, pageSize),
            PageSize = pageSize
        });

        var rows = (await connection
            .QueryAsync<AccessDenialRow>(sql, parameters, queryName: "AuditSql.DenialsPage", cancellationToken: cancellationToken)
            .ConfigureAwait(false)).ToList();

        var items = rows
            .Select(row => new AccessDenialItem(
                row.PublicId,
                row.PermissionCode,
                row.Route,
                row.HttpMethod,
                row.AttemptedBy,
                row.AttemptedByDisplay,
                row.EnforcementEnabled,
                row.Blocked,
                row.OccurredAtUtc))
            .ToList();

        var totalCount = rows.Count == 0 ? 0 : rows[0].TotalCount;

        return new PagedResult<AccessDenialItem>(items, totalCount, pageNumber, pageSize);
    }
}
