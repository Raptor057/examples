using Printing.Domain.Entities;

namespace Printing.Domain.Abstractions;

/// <summary>
/// El puerto de impresion. Todo lo que el resto del sistema sabe hacer con una impresora esta
/// aqui, y NADA de esto menciona a Zebra.
///
/// Tiene dos adaptadores (ADR-0004): el real, contra el SDK de Link-OS, y un simulador que
/// escribe el ZPL en disco. El simulador no es un mock de pruebas: es lo que deja levantar el
/// ejemplo -y desarrollar la aplicacion- sin una impresora enfrente.
/// </summary>
public interface IPrinterGateway
{
    /// <summary>Como se llama este adaptador. Sale en el health y en la pantalla, para que nadie confunda una impresion simulada con una real.</summary>
    string Name { get; }

    /// <summary>Las impresoras que el adaptador ve: colas del sistema y, si se pide, un barrido de red.</summary>
    Task<IReadOnlyList<DiscoveredPrinter>> DiscoverAsync(bool includeNetwork, CancellationToken cancellationToken);

    /// <summary>Manda el ZPL tal cual. No interpreta ni valida el contenido: eso ya paso aguas arriba.</summary>
    Task<PrintReceipt> SendAsync(PrinterTarget target, string zpl, CancellationToken cancellationToken);

    /// <summary>Estado de la impresora. Cada llamada pregunta de verdad; no hay cache.</summary>
    Task<PrinterStatusSnapshot> GetStatusAsync(PrinterTarget target, CancellationToken cancellationToken);

    /// <summary>
    /// Si este adaptador sabe dar un estado util. El simulador contesta que no: siempre dice
    /// "lista", asi que preguntarle antes de imprimir solo gastaria una llamada y daria una
    /// sensacion falsa de comprobacion.
    /// </summary>
    bool SupportsStatus { get; }
}

/// <summary>
/// Falla de comunicacion con la impresora: no responde, rechaza la conexion, se corta a media
/// escritura. Es un fallo ESPERADO -las impresoras de piso se apagan y se desconectan todo el
/// tiempo- y por eso tiene su propio tipo y su propio codigo HTTP, en vez de viajar como 500.
/// </summary>
public sealed class PrinterCommunicationException(string message, Exception? innerException = null)
    : Exception(message, innerException);
