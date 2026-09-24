using Printing.Domain.Abstractions;

namespace Printing.Infrastructure.Persistence;

/// <summary>
/// La fila TAL COMO LA DEVUELVE SQLite, no el modelo de dominio.
///
/// Existe por una razon muy concreta, y vale la pena saberla antes de tropezar con ella:
/// **SQLite no tiene ni booleano ni fecha**. Guarda 0/1 y texto. Dapper materializa un record
/// POSICIONAL buscando un constructor que case con los tipos que trae el lector, asi que contra
/// <c>PrintJobEntry(long, string, string?, int?, string, bool, string?, DateTime)</c> falla con:
///
///     A parameterless default constructor or one matching signature
///     (Int64 Id, String Target, ..., Int64 Succeeded, String CreatedAtUtc) is required
///
/// La salida NO es aflojar el modelo de dominio para que le cuadre a la base. Es esta: un tipo de
/// fila en Infrastructure que habla el idioma de SQLite, y una conversion explicita. El dominio no
/// se entera de en que motor esta guardado, que es justo lo que se quiere.
///
/// Las plantillas no necesitan uno porque <c>LabelTemplate</c> usa propiedades <c>init</c>: ahi
/// Dapper mapea por nombre de propiedad y convierte cada valor por separado. El problema es
/// exclusivo de los records posicionales.
/// </summary>
internal sealed class PrintJobRow
{
    public long Id { get; init; }
    public string Target { get; init; } = string.Empty;
    public string? TemplateCode { get; init; }
    public long? Dpi { get; init; }
    public string Zpl { get; init; } = string.Empty;

    /// <summary>0 o 1: SQLite no tiene booleano.</summary>
    public long Succeeded { get; init; }

    public string? Error { get; init; }

    /// <summary>Texto ISO-8601: SQLite no tiene fecha.</summary>
    public string CreatedAtUtc { get; init; } = string.Empty;

    public long? TemplateVersion { get; init; }
    public long? QueueItemId { get; init; }

    public PrintJobEntry ToEntry() => new(
        Id,
        Target,
        TemplateCode,
        Dpi is null ? null : (int)Dpi.Value,
        Zpl,
        Succeeded != 0,
        Error,
        DateTime.TryParse(
            CreatedAtUtc,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
            out var parsed)
            ? parsed
            : DateTime.MinValue,
        TemplateVersion is null ? null : (int)TemplateVersion.Value,
        QueueItemId);
}
