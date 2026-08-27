using Automation.Domain.Entities;

namespace Automation.Domain.Repositories;

/// <summary>
/// Acceso a la configuracion y al estado de las tareas. La implementacion vive en
/// Infrastructure; el despachador solo conoce esta interfaz.
/// </summary>
public interface IAutomatedTaskRepository
{
    /// <summary>
    /// EL RECLAMO. Selecciona y reclama en UNA sola sentencia las tareas vencidas que esta
    /// instancia gano.
    ///
    /// Leer primero y actualizar despues no sirve: entre las dos operaciones hay una ventana, y
    /// ahi caben las dos replicas. Con una sola sentencia, el motor decide y la otra instancia
    /// se lleva una lista vacia.
    /// </summary>
    /// <param name="knownCodes">
    /// Los codigos que el CODIGO conoce. Una fila con un codigo que no este aqui nunca se
    /// reclama: la base no puede dar de alta una tarea que no existe como clase.
    /// </param>
    /// <param name="staleClaimBeforeUtc">
    /// Un reclamo mas viejo que esto se considera abandonado y se puede retomar. Sin esto, una
    /// instancia que muere a media corrida congela su tarea para siempre.
    /// </param>
    Task<IReadOnlyList<ClaimedTask>> ClaimDueAsync(
        IReadOnlyCollection<string> knownCodes,
        DateTime nowUtc,
        DateTime staleClaimBeforeUtc,
        string claimedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reclama UNA tarea concreta ignorando su horario, que es lo que hace "ejecutar ahora".
    ///
    /// Lo que NO ignora es la pausa: la sentencia lleva IsEnabled = 1 igual que el reclamo
    /// automatico. Si el boton se saltara la pausa, pausar dejaria de ser una garantia y seria
    /// solo una sugerencia.
    /// </summary>
    Task<ClaimedTask?> ClaimSpecificAsync(
        string code,
        DateTime nowUtc,
        DateTime staleClaimBeforeUtc,
        string claimedBy,
        CancellationToken cancellationToken = default);

    /// <summary>Cierra la corrida: mueve el cursor, calcula la proxima y suelta el reclamo.</summary>
    Task CompleteRunAsync(
        string code,
        DateTime lastRunAtUtc,
        DateTime nextRunAtUtc,
        DateTime lastCutoffUtc,
        string lastStatus,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suelta el reclamo y nada mas. Se llama SIEMPRE en el finally del despachador, incluso
    /// despues de haber cerrado bien la corrida: si algo revento entre medias, esta es la
    /// llamada que evita que la tarea quede reclamada por un proceso que ya no existe.
    /// </summary>
    Task ReleaseClaimAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AutomatedTaskState>> ListAsync(
        IReadOnlyCollection<string> knownCodes,
        CancellationToken cancellationToken = default);

    Task<AutomatedTaskState?> FindAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pausa o reanuda. Al reanudar se recalcula la proxima corrida, porque la que tenia
    /// guardada quedo en el pasado mientras estuvo pausada.
    /// </summary>
    Task<bool> SetEnabledAsync(
        string code,
        bool isEnabled,
        string? pausedReason,
        DateTime? nextRunAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Da de alta las tareas del catalogo que falten. NO toca las que ya estan: pisar la
    /// configuracion existente reanudaria en cada despliegue las tareas que alguien pauso a
    /// proposito.
    /// </summary>
    Task<int> SeedMissingAsync(
        IReadOnlyList<AutomatedTaskSeed> seeds,
        CancellationToken cancellationToken = default);
}
