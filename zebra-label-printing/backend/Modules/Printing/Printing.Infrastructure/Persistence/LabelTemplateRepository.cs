using LabelPrinting.Shared.Persistence;
using Printing.Domain.Abstractions;
using Printing.Domain.Entities;

namespace Printing.Infrastructure.Persistence;

internal sealed class LabelTemplateRepository(SqlDbConnection connection) : ILabelTemplateRepository
{
    public async Task<IReadOnlyList<LabelTemplate>> ListAsync(int? dpi, bool? isActive, CancellationToken cancellationToken)
    {
        var rows = await connection
            .QueryAsync<LabelTemplate>(LabelTemplateSql.List, new { Dpi = dpi, IsActive = isActive }, cancellationToken)
            .ConfigureAwait(false);
        return [.. rows];
    }

    public Task<LabelTemplate?> FindAsync(string code, int dpi, CancellationToken cancellationToken)
        => connection.QueryFirstOrDefaultAsync<LabelTemplate>(
            LabelTemplateSql.Find, new { Code = code, Dpi = dpi }, cancellationToken);

    public async Task<bool> ExistsAsync(string code, int dpi, CancellationToken cancellationToken)
        => await connection.ExecuteScalarAsync<long>(
            LabelTemplateSql.Exists, new { Code = code, Dpi = dpi }, cancellationToken).ConfigureAwait(false) > 0;

    public async Task<LabelTemplate> CreateAsync(LabelTemplate template, CancellationToken cancellationToken)
    {
        var created = await connection.QueryFirstOrDefaultAsync<LabelTemplate>(LabelTemplateSql.Insert, new
        {
            template.Code,
            template.Name,
            template.Description,
            template.Body,
            template.Dpi,
            template.CreatedAtUtc,
        }, cancellationToken).ConfigureAwait(false);

        return created ?? throw new InvalidOperationException("El INSERT no devolvio la plantilla creada.");
    }

    public async Task<bool> UpdateAsync(LabelTemplate template, CancellationToken cancellationToken)
        => await connection.ExecuteAsync(LabelTemplateSql.Update, new
        {
            template.Code,
            template.Name,
            template.Description,
            template.Body,
            template.Dpi,
            UpdatedAtUtc = template.UpdatedAtUtc ?? DateTime.UtcNow,
        }, cancellationToken).ConfigureAwait(false) > 0;

    public async Task<bool> DeactivateAsync(string code, int dpi, CancellationToken cancellationToken)
        => await connection.ExecuteAsync(LabelTemplateSql.Deactivate,
            new { Code = code, Dpi = dpi, UpdatedAtUtc = DateTime.UtcNow }, cancellationToken).ConfigureAwait(false) > 0;

    public async Task<IReadOnlyList<LabelTemplateVersion>> HistoryAsync(string code, int dpi, CancellationToken cancellationToken)
    {
        var rows = await connection
            .QueryAsync<LabelTemplateVersionRow>(LabelTemplateSql.History, new { Code = code, Dpi = dpi }, cancellationToken)
            .ConfigureAwait(false);
        return [.. rows.Select(r => r.ToVersion())];
    }

    public async Task<LabelTemplateVersion?> FindVersionAsync(string code, int dpi, int version, CancellationToken cancellationToken)
    {
        var row = await connection
            .QueryFirstOrDefaultAsync<LabelTemplateVersionRow>(
                LabelTemplateSql.FindVersion, new { Code = code, Dpi = dpi, Version = version }, cancellationToken)
            .ConfigureAwait(false);
        return row?.ToVersion();
    }
}
