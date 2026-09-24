using System.Globalization;
using Printing.Domain.Entities;

namespace Printing.Infrastructure.Persistence;

/// <summary>
/// La fila como la devuelve SQLite. Mismo motivo que <see cref="PrintJobRow"/>: aqui no hay ni
/// booleano ni fecha ni enum, solo enteros y texto.
/// </summary>
internal sealed class PrintQueueRow
{
    public long Id { get; init; }
    public string TargetJson { get; init; } = string.Empty;
    public string TargetLabel { get; init; } = string.Empty;
    public string? TemplateCode { get; init; }
    public long? Dpi { get; init; }
    public string Zpl { get; init; } = string.Empty;
    public long Status { get; init; }
    public long Attempts { get; init; }
    public string NextAttemptAtUtc { get; init; } = string.Empty;
    public string? LastError { get; init; }
    public string CreatedAtUtc { get; init; } = string.Empty;
    public string? CompletedAtUtc { get; init; }

    public PrintQueueItem ToItem() => new()
    {
        Id = Id,
        TargetJson = TargetJson,
        TargetLabel = TargetLabel,
        TemplateCode = TemplateCode,
        Dpi = Dpi is null ? null : (int)Dpi.Value,
        Zpl = Zpl,
        Status = (PrintQueueStatus)Status,
        Attempts = (int)Attempts,
        NextAttemptAtUtc = Parse(NextAttemptAtUtc) ?? DateTime.MinValue,
        LastError = LastError,
        CreatedAtUtc = Parse(CreatedAtUtc) ?? DateTime.MinValue,
        CompletedAtUtc = Parse(CompletedAtUtc),
    };

    private static DateTime? Parse(string? value)
        => string.IsNullOrEmpty(value)
            ? null
            : DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var parsed)
                ? parsed
                : null;
}
