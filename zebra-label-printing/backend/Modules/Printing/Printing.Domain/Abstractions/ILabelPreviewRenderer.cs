namespace Printing.Domain.Abstractions;

/// <summary>
/// Convierte ZPL en una imagen para verla antes de gastar una etiqueta.
///
/// Es un puerto y no una llamada directa por una razon que importa: hoy lo resuelve un servicio de
/// TERCEROS por internet, y eso significa mandar el contenido de la etiqueta fuera de la red. El
/// dia que eso no sea aceptable, se escribe un renderizador local y la aplicacion ni se entera.
/// </summary>
public interface ILabelPreviewRenderer
{
    /// <summary>Si esta disponible. Viene apagado por omision; ver ADR-0009.</summary>
    bool IsEnabled { get; }

    Task<LabelPreview> RenderAsync(string zpl, int dpi, CancellationToken cancellationToken);
}

public sealed record LabelPreview(byte[] Content, string ContentType);

/// <summary>
/// La vista previa no se pudo generar. Es un fallo ESPERADO -esta apagada, el servicio no
/// contesta, el ZPL no le gusto- y por eso no viaja como 500.
/// </summary>
public sealed class PreviewUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
