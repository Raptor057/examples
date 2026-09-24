using System.Text.Json;
using Microsoft.Extensions.Logging;
using Printing.Domain.Abstractions;
using Printing.Domain.Entities;
using Printing.Domain.Rules;

namespace Printing.Application.Services;

/// <summary>
/// Una vuelta de la cola: toma lo vencido, lo reintenta, y anota el resultado.
///
/// Vive en Application y NO en el Host a proposito. El Host solo decide CADA CUANTO se llama
/// (es un detalle de hospedaje); que hacer en cada vuelta es logica de la aplicacion, y asi se
/// puede probar sin levantar un servicio en segundo plano.
/// </summary>
public sealed class PrintQueueWorker(
    IPrintQueue queue,
    IPrinterGateway gateway,
    IPrintJobLog jobLog,
    TimeProvider time,
    ILogger<PrintQueueWorker> logger)
{
    /// <summary>Cuantos trabajos se muerden por vuelta. Sin tope, una cola larga bloquea el despachador entero.</summary>
    public const int BatchSize = 10;

    /// <returns>Cuantos salieron en esta vuelta.</returns>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var due = await queue.ClaimDueAsync(BatchSize, now, cancellationToken).ConfigureAwait(false);
        if (due.Count == 0) return 0;

        var sent = 0;
        foreach (var item in due)
        {
            // Se corta limpio si alguien esta apagando el servicio: dejar un trabajo a medias es
            // peor que dejarlo pendiente, porque ya no se sabe si salio.
            if (cancellationToken.IsCancellationRequested) break;

            if (await TryOneAsync(item, cancellationToken).ConfigureAwait(false)) sent++;
        }

        return sent;
    }

    private async Task<bool> TryOneAsync(PrintQueueItem item, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var attempts = item.Attempts + 1;

        PrinterTarget? target;
        try
        {
            target = JsonSerializer.Deserialize<PrinterTarget>(item.TargetJson);
        }
        catch (JsonException ex)
        {
            // Un destino ilegible NO se reintenta: reintentar algo que no se puede ni leer es
            // gastar la cola para siempre. Se mata de una y se deja el motivo escrito.
            await queue.MarkFailedAsync(item.Id, PrintQueueRules.MaxAttempts,
                $"El destino guardado no se puede leer: {ex.Message}", now, dead: true, cancellationToken)
                .ConfigureAwait(false);
            return false;
        }

        if (target is null)
        {
            await queue.MarkFailedAsync(item.Id, PrintQueueRules.MaxAttempts,
                "El destino guardado esta vacio.", now, dead: true, cancellationToken).ConfigureAwait(false);
            return false;
        }

        try
        {
            await gateway.SendAsync(target, item.Zpl, cancellationToken).ConfigureAwait(false);
            await queue.MarkSentAsync(item.Id, now, cancellationToken).ConfigureAwait(false);
            await jobLog.RecordAsync(
                new PrintJobEntry(0, item.TargetLabel, item.TemplateCode, item.Dpi, item.Zpl, true, null, now, null, item.Id),
                cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Trabajo {Id} salio en el intento {Attempts}.", item.Id, attempts);
            return true;
        }
        catch (PrinterCommunicationException ex)
        {
            var dead = PrintQueueRules.IsExhausted(attempts);
            var next = dead ? now : PrintQueueRules.NextAttemptAt(attempts, now);

            await queue.MarkFailedAsync(item.Id, attempts, ex.Message, next, dead, cancellationToken).ConfigureAwait(false);

            if (dead)
            {
                // Se registra en la bitacora SOLO al morir. Anotar cada reintento llenaria la
                // bitacora de ruido y esconderia las impresiones de verdad.
                await jobLog.RecordAsync(
                    new PrintJobEntry(0, item.TargetLabel, item.TemplateCode, item.Dpi, item.Zpl, false,
                        $"Se agotaron los {PrintQueueRules.MaxAttempts} intentos. Ultimo error: {ex.Message}",
                        now, null, item.Id),
                    cancellationToken).ConfigureAwait(false);

                logger.LogWarning("Trabajo {Id} MUERTO tras {Attempts} intentos: {Error}", item.Id, attempts, ex.Message);
            }
            else
            {
                logger.LogInformation(
                    "Trabajo {Id} fallo (intento {Attempts}); siguiente a las {Next:HH:mm:ss}.", item.Id, attempts, next);
            }

            return false;
        }
    }
}
