using System.Globalization;
using Printing.Domain.Entities;

namespace Printing.Infrastructure.Persistence;

/// <summary>Fila del historial como la devuelve SQLite: las fechas llegan como texto.</summary>
internal sealed class LabelTemplateVersionRow
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public long Dpi { get; init; }
    public long Version { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Body { get; init; } = string.Empty;
    public string CreatedAtUtc { get; init; } = string.Empty;
    public string? ReplacedAtUtc { get; init; }

    public LabelTemplateVersion ToVersion() => new()
    {
        Id = Id,
        Code = Code,
        Dpi = (int)Dpi,
        Version = (int)Version,
        Name = Name,
        Description = Description,
        Body = Body,
        CreatedAtUtc = Parse(CreatedAtUtc) ?? DateTime.MinValue,
        ReplacedAtUtc = Parse(ReplacedAtUtc),
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
