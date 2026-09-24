namespace Printing.Domain.Entities;

/// <summary>Como se llega a una impresora. El dominio no sabe de sockets ni de colas de Windows.</summary>
public enum PrinterTransport
{
    /// <summary>Por red: direccion y puerto.</summary>
    Network = 0,

    /// <summary>Por una cola instalada en el sistema operativo donde corre la API.</summary>
    Installed = 1,
}

/// <summary>
/// A donde se manda una etiqueta. Un solo tipo para los dos transportes: quien imprime no tiene
/// por que ramificar, y el adaptador decide como abrir la conexion.
/// </summary>
public sealed record PrinterTarget
{
    public required PrinterTransport Transport { get; init; }

    /// <summary>IP o nombre DNS. Solo para <see cref="PrinterTransport.Network"/>.</summary>
    public string? Address { get; init; }

    /// <summary>Puerto TCP. El estandar de ZPL es 9100.</summary>
    public int Port { get; init; } = DefaultZplPort;

    /// <summary>Nombre de la cola. Solo para <see cref="PrinterTransport.Installed"/>.</summary>
    public string? QueueName { get; init; }

    public const int DefaultZplPort = 9100;

    /// <summary>Como se nombra en un log o en un mensaje de error.</summary>
    public string Describe() => Transport == PrinterTransport.Network
        ? $"{Address}:{Port}"
        : QueueName ?? "(sin nombre)";

    public static PrinterTarget Network(string address, int? port = null) => new()
    {
        Transport = PrinterTransport.Network,
        Address = address,
        Port = port is > 0 ? port.Value : DefaultZplPort,
    };

    public static PrinterTarget Installed(string queueName) => new()
    {
        Transport = PrinterTransport.Installed,
        QueueName = queueName,
    };
}

/// <summary>Una impresora que el sistema encontro, venga de la red o del sistema operativo.</summary>
public sealed record DiscoveredPrinter(
    string Name,
    PrinterTransport Transport,
    string? Address,
    bool IsDefault);

/// <summary>Acuse de un envio. No dice que la etiqueta SALIO: dice que los bytes se entregaron.</summary>
public sealed record PrintReceipt(
    string Target,
    int BytesSent,
    long ElapsedMs,
    DateTimeOffset SentAtUtc);

/// <summary>
/// Estado de la impresora, con las banderas tal como las reporta Link-OS.
/// <paramref name="Messages"/> es el texto legible ya armado: sin el, cada cliente inventa su
/// propia traduccion de las banderas y ninguna coincide.
/// </summary>
public sealed record PrinterStatusSnapshot(
    string Target,
    bool IsReadyToPrint,
    bool IsPaperOut,
    bool IsHeadOpen,
    bool IsPaused,
    bool IsRibbonOut,
    bool IsReceiveBufferFull,
    bool IsHeadTooHot,
    int LabelsRemainingInBatch,
    string Messages,
    DateTimeOffset ReadAtUtc);
