using Printing.Domain.Entities;

namespace Printing.Domain.Abstractions;

/// <summary>
/// La cola de impresion. Existe por una razon de piso muy concreta: una impresora apagada no
/// deberia perder la etiqueta. Sin cola, el operador se entera media hora despues de que la caja
/// se fue sin etiquetar.
/// </summary>
public interface IPrintQueue
{
    /// <summary>Encola un trabajo que ya fallo una vez, o uno que se pidio encolado de entrada.</summary>
    Task<long> EnqueueAsync(PrintQueueItem item, CancellationToken cancellationToken);

    /// <summary>
    /// Toma los trabajos vencidos para intentarlos. El <paramref name="max"/> acota cuanto se
    /// muerde de una vez: sin tope, una cola de mil trabajos bloquea el despachador entero.
    /// </summary>
    Task<IReadOnlyList<PrintQueueItem>> ClaimDueAsync(int max, DateTime now, CancellationToken cancellationToken);

    Task MarkSentAsync(long id, DateTime now, CancellationToken cancellationToken);

    /// <summary>Anota el fallo y programa el siguiente intento, o lo da por muerto si ya no quedan.</summary>
    Task MarkFailedAsync(long id, int attempts, string error, DateTime nextAttemptAtUtc, bool dead, CancellationToken cancellationToken);

    Task<bool> CancelAsync(long id, DateTime now, CancellationToken cancellationToken);

    Task<IReadOnlyList<PrintQueueItem>> ListAsync(PrintQueueStatus? status, int take, CancellationToken cancellationToken);

    /// <summary>Cuantos hay en cada estado. Es lo que el health necesita para avisar de una cola atorada.</summary>
    Task<IReadOnlyDictionary<PrintQueueStatus, int>> CountByStatusAsync(CancellationToken cancellationToken);
}
