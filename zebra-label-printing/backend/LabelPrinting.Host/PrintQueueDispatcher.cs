using Printing.Application.Services;

namespace LabelPrinting.Host;

/// <summary>
/// Despierta cada pocos segundos y le pide una vuelta a la cola. Eso es TODO lo que hace: la
/// logica de que intentar y cuando vive en <see cref="PrintQueueWorker"/>, en Application.
///
/// La separacion no es ceremonia. El Host decide CADA CUANTO -un detalle de hospedaje- y la
/// aplicacion decide QUE HACER, que es lo que se prueba. Un servicio en segundo plano con logica
/// adentro solo se puede probar levantandolo.
/// </summary>
public sealed class PrintQueueDispatcher(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<PrintQueueDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = configuration.GetValue("PrintQueue:Enabled", true);
        if (!enabled)
        {
            // Se dice en voz alta: una cola apagada acumula trabajos que nadie reintenta, y eso
            // pasado por alto se convierte en "las etiquetas no salen y no sé por qué".
            logger.LogWarning("La cola de impresion esta APAGADA (PrintQueue:Enabled = false). Nada se reintentara.");
            return;
        }

        var seconds = Math.Clamp(configuration.GetValue("PrintQueue:PollSeconds", 5), 1, 300);
        logger.LogInformation("Cola de impresion activa: una vuelta cada {Seconds} s.", seconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            // Un scope nuevo por vuelta: los repositorios son scoped y reusar el mismo durante
            // toda la vida del servicio dejaria conexiones abiertas para siempre.
            await using var scope = scopeFactory.CreateAsyncScope();

            try
            {
                var worker = scope.ServiceProvider.GetRequiredService<PrintQueueWorker>();
                await worker.RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Una vuelta que revienta NO puede matar al despachador: si se cae, la cola deja
                // de moverse y nadie se entera hasta que alguien pregunta por sus etiquetas.
                logger.LogError(ex, "Fallo una vuelta de la cola de impresion. Se reintenta en la siguiente.");
            }
        }
    }
}
