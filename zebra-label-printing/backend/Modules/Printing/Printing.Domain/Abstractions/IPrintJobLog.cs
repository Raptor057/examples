using Printing.Domain.Entities;

namespace Printing.Domain.Abstractions;

/// <summary>
/// La bitacora de lo que se mando a imprimir. Existe por una razon muy concreta de piso: cuando
/// alguien dice "esa etiqueta salio mal", lo primero que hace falta es el ZPL EXACTO que se
/// envio, no la plantilla ni los valores por separado.
/// </summary>
public interface IPrintJobLog
{
    Task RecordAsync(PrintJobEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyList<PrintJobEntry>> RecentAsync(int take, CancellationToken cancellationToken);
}

/// <param name="TemplateVersion">
/// Con que version del cuerpo se imprimio. Sin esto, saber que se uso "BOX_LABEL" no dice nada:
/// esa plantilla pudo cambiar diez veces desde entonces.
/// </param>
public sealed record PrintJobEntry(
    long Id,
    string Target,
    string? TemplateCode,
    int? Dpi,
    string Zpl,
    bool Succeeded,
    string? Error,
    DateTime CreatedAtUtc,
    int? TemplateVersion = null,
    long? QueueItemId = null);
