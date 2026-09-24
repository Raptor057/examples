using System.Text.Json;
using Printing.Domain.Abstractions;
using Printing.Domain.Entities;
using Printing.Domain.Rules;

namespace Printing.Application.Services;

/// <summary>
/// El acto de imprimir, en un solo sitio: preflight, envio, encolado y bitacora.
///
/// Vive aparte de los handlers porque lo usan TRES: imprimir una plantilla, mandar ZPL a mano, y
/// el despachador cuando reintenta lo encolado. Sin esto, la misma secuencia estaria escrita tres
/// veces y se desincronizaria a la primera correccion.
/// </summary>
public sealed class PrintDispatchService(
    IPrinterGateway gateway,
    IPrintQueue queue,
    IPrintJobLog jobLog,
    TimeProvider time)
{
    /// <param name="queueOnFailure">
    /// Si la impresora no contesta, encolar para reintentar en vez de rendirse. Por omision SI:
    /// perder una etiqueta porque la impresora estaba apagada un minuto es el fallo que este
    /// servicio existe para evitar.
    /// </param>
    public async Task<PrintDispatchResult> SendAsync(
        PrinterTarget target,
        string zpl,
        string? templateCode,
        int? dpi,
        int? templateVersion,
        bool queueOnFailure,
        bool preflight,
        CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow().UtcDateTime;

        // --- Preflight ---------------------------------------------------------------------
        // Preguntar ANTES cuesta un viaje, y evita el peor desenlace: los bytes entran al buffer
        // de una impresora sin papel, la API contesta que si, y la etiqueta no existe.
        if (preflight && gateway.SupportsStatus)
        {
            PrinterStatusSnapshot? status = null;
            try
            {
                status = await gateway.GetStatusAsync(target, cancellationToken).ConfigureAwait(false);
            }
            catch (PrinterCommunicationException)
            {
                // Si ni siquiera contesta el estado, no se rechaza aqui: se deja seguir y que el
                // envio falle con su propio mensaje. Rechazar en el preflight daria un error sobre
                // el estado cuando el problema es que la impresora no esta.
            }

            if (status is not null)
            {
                var blocking = PreflightRules.BlockingReason(status);
                if (blocking is not null)
                {
                    // Sin papel o con el cabezal abierto SE ENCOLA: se arregla en cuanto alguien
                    // lo atienda, y para entonces la etiqueta debe seguir esperando.
                    if (queueOnFailure && PreflightRules.IsTemporary(status))
                    {
                        var queuedId = await EnqueueAsync(target, zpl, templateCode, dpi, blocking, now, cancellationToken)
                            .ConfigureAwait(false);
                        await LogAsync(target, templateCode, dpi, templateVersion, zpl, false, blocking, queuedId, now, cancellationToken)
                            .ConfigureAwait(false);
                        return PrintDispatchResult.Queued(queuedId, blocking);
                    }

                    await LogAsync(target, templateCode, dpi, templateVersion, zpl, false, blocking, null, now, cancellationToken)
                        .ConfigureAwait(false);
                    return PrintDispatchResult.Rejected(blocking);
                }
            }
        }

        // --- Envio -------------------------------------------------------------------------
        try
        {
            var receipt = await gateway.SendAsync(target, zpl, cancellationToken).ConfigureAwait(false);
            await LogAsync(target, templateCode, dpi, templateVersion, zpl, true, null, null, now, cancellationToken)
                .ConfigureAwait(false);
            return PrintDispatchResult.Printed(receipt);
        }
        catch (PrinterCommunicationException ex)
        {
            if (!queueOnFailure)
            {
                await LogAsync(target, templateCode, dpi, templateVersion, zpl, false, ex.Message, null, now, cancellationToken)
                    .ConfigureAwait(false);
                return PrintDispatchResult.Rejected(ex.Message);
            }

            var queueId = await EnqueueAsync(target, zpl, templateCode, dpi, ex.Message, now, cancellationToken)
                .ConfigureAwait(false);
            await LogAsync(target, templateCode, dpi, templateVersion, zpl, false, ex.Message, queueId, now, cancellationToken)
                .ConfigureAwait(false);
            return PrintDispatchResult.Queued(queueId, ex.Message);
        }
    }

    private Task<long> EnqueueAsync(
        PrinterTarget target, string zpl, string? code, int? dpi, string error, DateTime now, CancellationToken cancellationToken)
        => queue.EnqueueAsync(new PrintQueueItem
        {
            TargetJson = JsonSerializer.Serialize(target),
            TargetLabel = target.Describe(),
            TemplateCode = code,
            Dpi = dpi,
            Zpl = zpl,
            Attempts = 1,
            NextAttemptAtUtc = PrintQueueRules.NextAttemptAt(1, now),
            LastError = error,
            CreatedAtUtc = now,
        }, cancellationToken);

    private Task LogAsync(
        PrinterTarget target, string? code, int? dpi, int? version, string zpl,
        bool ok, string? error, long? queueId, DateTime now, CancellationToken cancellationToken)
        => jobLog.RecordAsync(
            new PrintJobEntry(0, target.Describe(), code, dpi, zpl, ok, error, now, version, queueId),
            cancellationToken);
}

/// <summary>Los tres desenlaces posibles, sin ambiguedad. Ver <see cref="PrintDisposition"/>.</summary>
public sealed record PrintDispatchResult(
    PrintDisposition Disposition,
    PrintReceipt? Receipt,
    long? QueueItemId,
    string? Message)
{
    public static PrintDispatchResult Printed(PrintReceipt receipt)
        => new(PrintDisposition.Printed, receipt, null, null);

    public static PrintDispatchResult Queued(long queueItemId, string reason)
        => new(PrintDisposition.Queued, null, queueItemId, reason);

    public static PrintDispatchResult Rejected(string reason)
        => new(PrintDisposition.Rejected, null, null, reason);
}
