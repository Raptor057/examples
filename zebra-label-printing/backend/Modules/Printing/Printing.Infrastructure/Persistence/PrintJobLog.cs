using LabelPrinting.Shared.Persistence;
using Printing.Domain.Abstractions;

namespace Printing.Infrastructure.Persistence;

internal sealed class PrintJobLog(SqlDbConnection connection) : IPrintJobLog
{
    public Task RecordAsync(PrintJobEntry entry, CancellationToken cancellationToken)
        => connection.ExecuteAsync(PrintJobSql.Insert, new
        {
            entry.Target,
            entry.TemplateCode,
            entry.Dpi,
            entry.Zpl,
            entry.Succeeded,
            entry.Error,
            entry.CreatedAtUtc,
            entry.TemplateVersion,
            entry.QueueItemId,
        }, cancellationToken);

    public async Task<IReadOnlyList<PrintJobEntry>> RecentAsync(int take, CancellationToken cancellationToken)
    {
        // PrintJobRow y no PrintJobEntry: SQLite devuelve el booleano como entero y la fecha como
        // texto, y un record posicional no se deja materializar asi. Ver PrintJobRow.
        var rows = await connection
            .QueryAsync<PrintJobRow>(PrintJobSql.Recent, new { Take = take }, cancellationToken)
            .ConfigureAwait(false);
        return [.. rows.Select(r => r.ToEntry())];
    }
}
