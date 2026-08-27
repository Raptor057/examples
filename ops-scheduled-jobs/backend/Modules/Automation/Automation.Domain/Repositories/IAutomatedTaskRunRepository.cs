using Automation.Domain.Entities;

namespace Automation.Domain.Repositories;

/// <summary>Bitacora de corridas. Solo se agrega y se cierra; nunca se borra.</summary>
public interface IAutomatedTaskRunRepository
{
    /// <summary>
    /// Abre la corrida ANTES de ejecutar nada, en estado RUNNING. Escribirla al final seria mas
    /// simple y perderia justo el caso interesante: una tarea que se cae sin cerrar deja su
    /// fila RUNNING a la vista, y eso es la senal de que algo se murio a media ejecucion.
    /// </summary>
    Task<long> StartAsync(AutomatedTaskRunStart start, CancellationToken cancellationToken = default);

    Task FinishAsync(
        long runId,
        string status,
        int itemsProcessed,
        int itemsFailed,
        string message,
        DateTime finishedAtUtc,
        DateTime? cursorAfterUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cierra como FAILED las corridas que quedaron abiertas de un reclamo abandonado. Se llama
    /// al retomar un reclamo vencido: la corrida anterior no va a cerrarse sola.
    /// </summary>
    Task<int> AbandonOpenRunsAsync(
        string taskCode,
        DateTime nowUtc,
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>Bitacora paginada EN SERVIDOR: OFFSET/FETCH mas el total real.</summary>
    Task<PagedResult<AutomatedTaskRun>> GetPageAsync(
        string taskCode,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>La ultima corrida de cada tarea, para la pantalla de lista.</summary>
    Task<IReadOnlyList<AutomatedTaskRun>> GetLatestPerTaskAsync(
        IReadOnlyCollection<string> taskCodes,
        CancellationToken cancellationToken = default);
}
