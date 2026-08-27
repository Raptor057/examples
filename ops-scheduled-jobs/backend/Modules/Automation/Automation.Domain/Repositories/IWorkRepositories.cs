using Automation.Domain.Entities;
using Automation.Domain.Tasks;

namespace Automation.Domain.Repositories;

/// <summary>
/// Datos de refresh-order-summary. Las tres firmas de lectura reciben la VENTANA entera: la
/// tarea no le pasa fechas sueltas al repositorio ni el repositorio calcula ninguna. Asi no hay
/// dos sitios donde equivocarse con el rango.
/// </summary>
public interface IOrderSummaryRepository
{
    /// <summary>
    /// Eventos ocurridos dentro de la ventana, ordenados por (OccurredAtUtc, Id). El orden es
    /// parte del contrato: sin el, "hasta donde salio bien" no significa nada.
    /// </summary>
    Task<IReadOnlyList<OrderEventRecord>> GetEventsInWindowAsync(
        TaskWindow window, int maxItems, CancellationToken cancellationToken = default);

    /// <summary>
    /// Escribe el resumen de un dia y canal. Es un MERGE por (dia, canal), asi que reprocesar la
    /// misma ventana deja el mismo resultado. Esa idempotencia es lo que permite que el cursor
    /// se quede corto sin consecuencias.
    /// </summary>
    Task UpsertDailySummaryAsync(
        DateOnly summaryDate,
        string channelCode,
        int orderCount,
        int units,
        decimal amount,
        DateTime refreshedAtUtc,
        CancellationToken cancellationToken = default);
}

/// <summary>Datos de purge-expired-temp-files.</summary>
public interface ITempFileRepository
{
    /// <summary>Temporales cuya EXPIRACION cayo dentro de la ventana, ordenados por (ExpiresAtUtc, Id).</summary>
    Task<IReadOnlyList<TempFileRecord>> GetExpiredInWindowAsync(
        TaskWindow window, int maxItems, CancellationToken cancellationToken = default);

    /// <summary>Baja LOGICA. Nunca DELETE fisico (rules/database-conventions).</summary>
    Task<int> PurgeAsync(long id, DateTime purgedAtUtc, CancellationToken cancellationToken = default);
}

/// <summary>Datos de retry-pending-dispatches.</summary>
public interface IPendingDispatchRepository
{
    /// <summary>Envios encolados dentro de la ventana que siguen sin resolver, ordenados por (QueuedAtUtc, Id).</summary>
    Task<IReadOnlyList<PendingDispatchRecord>> GetPendingInWindowAsync(
        TaskWindow window, int maxItems, CancellationToken cancellationToken = default);

    Task MarkSentAsync(long id, int attempts, DateTime attemptedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// El intento fallo. <paramref name="isDead"/> distingue "sigue pendiente" de "agoto los
    /// reintentos": el primero FRENA el cursor y el segundo lo deja pasar, porque un elemento
    /// que ya nadie va a reintentar no puede bloquear la ventana para siempre.
    /// </summary>
    Task MarkFailedAsync(
        long id, int attempts, string error, DateTime attemptedAtUtc, bool isDead,
        CancellationToken cancellationToken = default);
}
