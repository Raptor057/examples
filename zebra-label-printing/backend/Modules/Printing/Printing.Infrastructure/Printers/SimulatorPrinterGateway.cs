using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Printing.Domain.Abstractions;
using Printing.Domain.Entities;

namespace Printing.Infrastructure.Printers;

/// <summary>
/// Impresora de mentira: guarda cada etiqueta como un .zpl en una carpeta y devuelve un estado
/// sano. Es lo que hace que este ejemplo se pueda levantar y recorrer sin una Zebra enfrente.
///
/// No es un mock de pruebas —tambien sirve para desarrollar la aplicacion cliente— y por eso vive
/// en Infrastructure y no en el proyecto de pruebas. El ZPL que escribe es el REAL: se puede
/// pegar en <see href="http://labelary.com/viewer.html">Labelary</see> y ver la etiqueta.
///
/// Miente en una cosa, a proposito: <see cref="GetStatusAsync"/> siempre contesta lista. Un
/// simulador que finge quedarse sin papel esconderia problemas reales detras de un estado
/// inventado.
/// </summary>
public sealed class SimulatorPrinterGateway(
    IOptions<PrintingOptions> options,
    ILogger<SimulatorPrinterGateway> logger) : IPrinterGateway
{
    private readonly PrintingOptions _options = options.Value;

    public string Name => "simulator";

    /// <summary>No. Siempre contesta "lista", asi que un preflight contra el simulador solo daria
    /// una sensacion falsa de comprobacion.</summary>
    public bool SupportsStatus => false;

    public Task<IReadOnlyList<DiscoveredPrinter>> DiscoverAsync(bool includeNetwork, CancellationToken cancellationToken)
    {
        IReadOnlyList<DiscoveredPrinter> printers =
        [
            new("SIMULADOR-203", PrinterTransport.Installed, null, true),
            new("SIMULADOR-300", PrinterTransport.Installed, null, false),
        ];

        if (includeNetwork)
        {
            printers = [.. printers, new DiscoveredPrinter("SIMULADOR-RED", PrinterTransport.Network, "127.0.0.1", false)];
        }

        return Task.FromResult(printers);
    }

    public async Task<PrintReceipt> SendAsync(PrinterTarget target, string zpl, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        Directory.CreateDirectory(_options.SimulatorOutputPath);

        var safeTarget = string.Concat(target.Describe().Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}_{safeTarget}.zpl";
        var fullPath = Path.Combine(_options.SimulatorOutputPath, fileName);

        var payload = Encoding.UTF8.GetBytes(zpl);
        await File.WriteAllBytesAsync(fullPath, payload, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        logger.LogInformation("Etiqueta simulada en {Path} ({Bytes} bytes)", fullPath, payload.Length);
        return new PrintReceipt(target.Describe(), payload.Length, stopwatch.ElapsedMilliseconds, DateTimeOffset.UtcNow);
    }

    public Task<PrinterStatusSnapshot> GetStatusAsync(PrinterTarget target, CancellationToken cancellationToken)
        => Task.FromResult(new PrinterStatusSnapshot(
            target.Describe(),
            IsReadyToPrint: true,
            IsPaperOut: false,
            IsHeadOpen: false,
            IsPaused: false,
            IsRibbonOut: false,
            IsReceiveBufferFull: false,
            IsHeadTooHot: false,
            LabelsRemainingInBatch: 0,
            Messages: "Simulador: la impresora siempre contesta lista.",
            ReadAtUtc: DateTimeOffset.UtcNow));
}
