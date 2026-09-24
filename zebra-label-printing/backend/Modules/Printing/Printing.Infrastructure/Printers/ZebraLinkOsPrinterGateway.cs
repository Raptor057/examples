using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using Printing.Domain.Abstractions;
using Printing.Domain.Entities;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;
using Zebra.Sdk.Printer.Discovery;

// El SDK tiene su propio DiscoveredPrinter y choca con el del dominio. Los alias dejan a la
// vista cual es cual, que es exactamente el trabajo de este archivo: traducir el vocabulario
// de Zebra al nuestro.
using DomainPrinter = Printing.Domain.Entities.DiscoveredPrinter;
using SdkPrinter = Zebra.Sdk.Printer.Discovery.DiscoveredPrinter;

namespace Printing.Infrastructure.Printers;

/// <summary>
/// El adaptador real, contra el SDK de Link-OS 5.0.3685. Este es el UNICO archivo del proyecto
/// que menciona a Zebra. Todo lo demas habla con <see cref="IPrinterGateway"/>, y por eso cambiar
/// de marca de impresora seria escribir otro archivo como este, no tocar la aplicacion.
///
/// Tres cosas del SDK que condicionan todo lo que hay aqui, y que no se ven leyendo la API:
///
/// 1. Es SINCRONO. No existe OpenAsync ni WriteAsync. Por eso cada operacion va envuelta en
///    Task.Run con su CancellationToken: es la unica forma de no bloquear el hilo de la peticion.
/// 2. Close() TAMBIEN lanza ConnectionException. Si no se traga, un fallo al cerrar tapa el error
///    real del envio y el usuario lee "no se pudo cerrar la conexion" cuando lo que pasa es que
///    la impresora esta apagada.
/// 3. El estado se consulta de verdad en CADA llamada. El propio SDK recomienda guardar una copia
///    si se van a leer varios valores; aqui se lee una vez y se copia entera.
/// </summary>
public sealed class ZebraLinkOsPrinterGateway(IOptions<PrintingOptions> options) : IPrinterGateway
{
    private readonly PrintingOptions _options = options.Value;

    public string Name => "zebra";

    public bool SupportsStatus => true;

    public Task<IReadOnlyList<DomainPrinter>> DiscoverAsync(bool includeNetwork, CancellationToken cancellationToken)
        => Task.Run<IReadOnlyList<DomainPrinter>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var found = new List<DomainPrinter>();

            try
            {
                // GetZebraDriverPrinters y no la lista de colas del sistema: esta devuelve SOLO
                // las Zebra. Enumerar todas las impresoras de Windows obliga al cliente a filtrar
                // por nombre, a ojo, y siempre se le cuela el "Microsoft Print to PDF".
                foreach (var driver in UsbDiscoverer.GetZebraDriverPrinters())
                {
                    found.Add(new DomainPrinter(
                        driver.PrinterName ?? driver.Address, PrinterTransport.Installed, null, false));
                }
            }
            catch (ConnectionException ex)
            {
                throw new PrinterCommunicationException(
                    $"No se pudieron listar las impresoras instaladas. {ex.Message}", ex);
            }
            catch (DiscoveryException ex)
            {
                throw new PrinterCommunicationException(
                    $"No se pudieron listar las impresoras instaladas. {ex.Message}", ex);
            }

            if (!includeNetwork) return found;

            try
            {
                var handler = new CollectingDiscoveryHandler();
                NetworkDiscoverer.FindPrinters(handler);
                handler.WaitForCompletion(TimeSpan.FromSeconds(_options.DiscoveryTimeoutSeconds));
                found.AddRange(handler.Printers);
            }
            catch (DiscoveryException ex)
            {
                throw new PrinterCommunicationException($"Fallo el barrido de red. {ex.Message}", ex);
            }

            return found;
        }, cancellationToken);

    public Task<PrintReceipt> SendAsync(PrinterTarget target, string zpl, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var payload = Encoding.UTF8.GetBytes(zpl);
            var stopwatch = Stopwatch.StartNew();
            Connection? connection = null;

            try
            {
                connection = OpenConnection(target);
                connection.Write(payload);
                stopwatch.Stop();

                return new PrintReceipt(
                    target.Describe(), payload.Length, stopwatch.ElapsedMilliseconds, DateTimeOffset.UtcNow);
            }
            catch (ConnectionException ex)
            {
                throw new PrinterCommunicationException(
                    $"No se pudo enviar la etiqueta a {target.Describe()}. {ex.Message}", ex);
            }
            finally
            {
                CloseQuietly(connection);
            }
        }, cancellationToken);

    public Task<PrinterStatusSnapshot> GetStatusAsync(PrinterTarget target, CancellationToken cancellationToken)
        => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            Connection? connection = null;

            try
            {
                connection = OpenConnection(target);

                // CAMBIO DE RUPTURA 4.x -> 5.x: antes esto era `new PrinterStatus(connection)`.
                // En la 5 la clase es ABSTRACTA (sus concretas, PrinterStatusZpl y
                // PrinterStatusCpcl, son internas), asi que el estado SOLO se obtiene por la
                // fabrica. Cuesta una consulta extra -la que averigua el lenguaje de control- y
                // no hay forma de saltarsela. Ver docs/LINK-OS-SDK.md.
                var printer = ZebraPrinterFactory.GetInstance(connection);
                var status = printer.GetCurrentStatus();

                // El texto legible ya viene hecho. Sin esto, cada cliente inventa su propia
                // traduccion de las banderas y ninguna coincide con la de al lado.
                var messages = new PrinterStatusMessages(status).GetStatusMessage();

                return new PrinterStatusSnapshot(
                    target.Describe(),
                    status.isReadyToPrint,
                    status.isPaperOut,
                    status.isHeadOpen,
                    status.isPaused,
                    status.isRibbonOut,
                    status.isReceiveBufferFull,
                    status.isHeadTooHot,
                    status.labelsRemainingInBatch,
                    messages is null ? string.Empty : string.Join(" ", messages),
                    DateTimeOffset.UtcNow);
            }
            catch (ConnectionException ex)
            {
                throw new PrinterCommunicationException(
                    $"No se pudo consultar el estado de {target.Describe()}. {ex.Message}", ex);
            }
            catch (ZebraPrinterLanguageUnknownException ex)
            {
                // La fabrica no logro determinar si la impresora habla ZPL o CPCL. Pasa con
                // equipos que no son Zebra escuchando en el 9100, que es mas comun de lo que
                // parece: cualquier impresora de red acepta la conexion.
                throw new PrinterCommunicationException(
                    $"En {target.Describe()} hay algo escuchando, pero no parece una impresora Zebra. {ex.Message}", ex);
            }
            finally
            {
                CloseQuietly(connection);
            }
        }, cancellationToken);

    private Connection OpenConnection(PrinterTarget target)
    {
        Connection connection = target.Transport switch
        {
            // Los constructores con tiempos de espera existen justo para esto: los del SDK son
            // generosos y dejan la peticion HTTP colgada mucho mas de lo razonable.
            PrinterTransport.Network => new TcpConnection(
                target.Address!, target.Port, _options.NetworkTimeoutMs, _options.NetworkTimeoutMs),

            // DriverPrinterConnection habla con la cola instalada, y como es una Connection
            // normal, la impresora instalada TAMBIEN puede dar estado. Mandar los bytes a mano
            // por el spooler de Windows, que es lo habitual, no permite eso.
            PrinterTransport.Installed => new DriverPrinterConnection(target.QueueName!),

            _ => throw new PrinterCommunicationException($"Transporte no soportado: {target.Transport}."),
        };

        connection.Open();
        return connection;
    }

    /// <summary>Cerrar tambien puede fallar, y ese fallo no debe tapar el de verdad.</summary>
    private static void CloseQuietly(Connection? connection)
    {
        if (connection is null) return;
        try
        {
            connection.Close();
        }
        catch (ConnectionException)
        {
            // Intencionalmente ignorado: ver el punto 2 del comentario de la clase.
        }
    }

    /// <summary>
    /// El descubrimiento del SDK avisa por callback y no devuelve una lista: hay que recogerla y
    /// esperar a que termine.
    /// </summary>
    private sealed class CollectingDiscoveryHandler : DiscoveryHandler
    {
        private readonly List<DomainPrinter> _printers = [];
        private readonly ManualResetEventSlim _done = new(false);

        public IReadOnlyList<DomainPrinter> Printers
        {
            get { lock (_printers) return [.. _printers]; }
        }

        public void FoundPrinter(SdkPrinter printer)
        {
            lock (_printers)
            {
                _printers.Add(new DomainPrinter(
                    printer.Address, PrinterTransport.Network, printer.Address, false));
            }
        }

        public void DiscoveryFinished() => _done.Set();

        public void DiscoveryError(string message) => _done.Set();

        public void WaitForCompletion(TimeSpan timeout) => _done.Wait(timeout);
    }
}
