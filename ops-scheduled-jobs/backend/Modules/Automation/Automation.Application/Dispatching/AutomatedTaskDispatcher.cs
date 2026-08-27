using Automation.Domain.Entities;
using Automation.Domain.Repositories;
using Automation.Domain.Scheduling;
using Automation.Domain.Tasks;
using Microsoft.Extensions.Logging;

namespace Automation.Application.Dispatching;

/// <summary>Como termino un intento de despachar UNA tarea.</summary>
public enum DispatchOutcome
{
    /// <summary>El codigo no esta en el catalogo. La base no puede inventar tareas.</summary>
    UnknownTask,

    /// <summary>Esta en el catalogo pero no hay fila de configuracion todavia.</summary>
    NotConfigured,

    /// <summary>Esta pausada. "Ejecutar ahora" tambien respeta esto.</summary>
    Paused,

    /// <summary>Otra instancia la tiene reclamada y su reclamo sigue vigente.</summary>
    AlreadyClaimed,

    /// <summary>Se ejecuto. El resultado dice como le fue.</summary>
    Executed
}

/// <summary>
/// Lo que devuelve despachar una tarea concreta. Lleva la VENTANA que se cubrio porque quien
/// dispara a mano necesita ver que rango entro, no solo cuantos elementos salieron.
/// </summary>
public sealed record DispatchReport(string Code, DispatchOutcome Outcome, TaskRunResult? Result, TaskWindow? Window);

/// <summary>
/// EL DESPACHADOR. Uno solo para todas las tareas, no uno por tarea.
///
/// Un proceso residente por tarea parece mas simple con dos tareas y es insostenible con diez:
/// diez temporizadores, diez cableados, diez sitios donde arreglar el mismo bug de reclamo.
/// Aqui agregar una tarea es escribir una clase que implemente <see cref="IAutomatedTask"/> y
/// registrarla; este archivo no se toca. Las tres tareas del ejemplo existen justamente para
/// que eso se pueda comprobar en vez de tener que creerlo.
///
/// El orden de las operaciones no es negociable:
///
/// 1. **Reclamar y seleccionar en UNA sentencia.** Leer primero y actualizar despues deja una
///    ventana en medio, y ahi caben las dos replicas.
/// 2. **Abrir la corrida en la bitacora ANTES de ejecutar.** Una tarea que se muere deja su
///    fila RUNNING a la vista.
/// 3. **La ventana sale del cursor**, nunca del reloj.
/// 4. **El cursor avanza solo hasta donde salio bien**, y ademas se recorta aqui para que una
///    tarea no lo pueda mover mas alla de lo que su ventana cubrio.
/// 5. **Soltar el reclamo SIEMPRE, en el finally.**
/// </summary>
public sealed class AutomatedTaskDispatcher
{
    private readonly IAutomatedTaskRepository _tasks;
    private readonly IAutomatedTaskRunRepository _runs;
    private readonly SchedulingTimeZone _timeZone;
    private readonly DispatcherOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<AutomatedTaskDispatcher> _logger;
    private readonly IReadOnlyDictionary<string, IAutomatedTask> _implementations;

    public AutomatedTaskDispatcher(
        IAutomatedTaskRepository tasks,
        IAutomatedTaskRunRepository runs,
        IEnumerable<IAutomatedTask> implementations,
        SchedulingTimeZone timeZone,
        DispatcherOptions options,
        TimeProvider clock,
        ILogger<AutomatedTaskDispatcher> logger)
    {
        _tasks = tasks;
        _runs = runs;
        _timeZone = timeZone;
        _options = options;
        _clock = clock;
        _logger = logger;

        // El contenedor entrega TODAS las implementaciones registradas y aqui se indexan por
        // codigo. Es el unico sitio donde el despachador "conoce" las tareas, y no las conoce
        // por nombre: las recibe.
        _implementations = implementations.ToDictionary(task => task.Code, StringComparer.Ordinal);
    }

    /// <summary>
    /// Comprueba que cada tarea del catalogo tenga su clase registrada. Se llama al arrancar.
    ///
    /// Sin esto, una tarea declarada en el catalogo pero sin registrar en el contenedor pasa
    /// desapercibida: aparece en la pantalla, se agenda, se reclama, y falla cada vez que le
    /// toca correr. Falla, ademas, en la bitacora y no en la consola, asi que se descubre tarde.
    /// Es mas barato no arrancar.
    /// </summary>
    public void EnsureEveryCatalogTaskIsRegistered()
    {
        var missing = AutomatedTaskCatalog.Codes
            .Where(code => !_implementations.ContainsKey(code))
            .ToList();

        if (missing.Count > 0)
            throw new InvalidOperationException(
                "Estas tareas estan en el catalogo pero no tienen implementacion registrada: " +
                string.Join(", ", missing) +
                ". Registralas en el ServiceCollectionEx de Infrastructure.");

        var orphans = _implementations.Keys
            .Where(code => !AutomatedTaskCatalog.Contains(code))
            .ToList();

        if (orphans.Count > 0)
            throw new InvalidOperationException(
                "Estas implementaciones no estan declaradas en el catalogo: " +
                string.Join(", ", orphans) +
                ". El catalogo en codigo es la fuente de verdad de que tareas existen.");
    }

    /// <summary>
    /// Un ciclo del despachador: reclama todo lo vencido y lo ejecuta. Devuelve lo que corrio
    /// ESTA instancia.
    /// </summary>
    public async Task<IReadOnlyList<DispatchReport>> DispatchDueAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = _clock.GetUtcNow().UtcDateTime;
        var staleBeforeUtc = nowUtc - _options.StaleClaimAfter;

        // Se le pasan los codigos del CATALOGO. Una fila con un codigo que el codigo no conoce
        // nunca entra en el reclamo: no habria clase que ejecutar.
        var claimed = await _tasks.ClaimDueAsync(
            AutomatedTaskCatalog.Codes, nowUtc, staleBeforeUtc, _options.InstanceId, cancellationToken)
            .ConfigureAwait(false);

        if (claimed.Count == 0) return [];

        var reports = new List<DispatchReport>(claimed.Count);
        foreach (var task in claimed)
        {
            // Las tareas se ejecutan una tras otra a proposito: en paralelo, dos tareas que
            // tocan la misma tabla competirian entre si dentro de la MISMA replica, que es
            // exactamente lo que el reclamo evita entre replicas distintas.
            var executed = await ExecuteClaimedAsync(task, triggeredByUser: null, cancellationToken).ConfigureAwait(false);
            reports.Add(new DispatchReport(task.State.Code, DispatchOutcome.Executed, executed.Result, executed.Window));
        }

        return reports;
    }

    /// <summary>
    /// "Ejecutar ahora". Se salta el HORARIO, que es justo lo que se le pide, y NO se salta la
    /// pausa, que es lo que casi siempre se hace mal.
    ///
    /// Un boton que ignora la pausa convierte pausar en una sugerencia: alguien apaga la tarea
    /// porque el sistema de destino esta caido, otro le da a "ejecutar ahora" para probar, y el
    /// motivo por el que estaba pausada sigue ahi.
    /// </summary>
    public async Task<DispatchReport> RunNowAsync(
        string code, string triggeredByUser, CancellationToken cancellationToken = default)
    {
        if (!AutomatedTaskCatalog.Contains(code))
            return new DispatchReport(code, DispatchOutcome.UnknownTask, null, null);

        var nowUtc = _clock.GetUtcNow().UtcDateTime;
        var staleBeforeUtc = nowUtc - _options.StaleClaimAfter;

        var claimed = await _tasks.ClaimSpecificAsync(
            code, nowUtc, staleBeforeUtc, _options.InstanceId, cancellationToken).ConfigureAwait(false);

        if (claimed is null)
        {
            // El reclamo no distingue por si solo entre "pausada", "ya reclamada" y "no existe".
            // Se consulta el estado para poder decirle a quien apreto el boton por que no paso
            // nada: un boton que no hace nada y no explica se reporta como bug.
            var state = await _tasks.FindAsync(code, cancellationToken).ConfigureAwait(false);
            if (state is null) return new DispatchReport(code, DispatchOutcome.NotConfigured, null, null);
            if (!state.IsEnabled) return new DispatchReport(code, DispatchOutcome.Paused, null, null);
            return new DispatchReport(code, DispatchOutcome.AlreadyClaimed, null, null);
        }

        var executed = await ExecuteClaimedAsync(claimed, triggeredByUser, cancellationToken).ConfigureAwait(false);
        return new DispatchReport(code, DispatchOutcome.Executed, executed.Result, executed.Window);
    }

    private sealed record ExecutedRun(TaskRunResult Result, TaskWindow Window);

    private async Task<ExecutedRun> ExecuteClaimedAsync(
        ClaimedTask claimed, string? triggeredByUser, CancellationToken cancellationToken)
    {
        var state = claimed.State;
        var nowUtc = _clock.GetUtcNow().UtcDateTime;
        var window = BuildWindow(state.LastCutoffUtc, nowUtc, _options.MaxWindow);

        try
        {
            if (claimed.TookOverStaleClaim)
            {
                // El reclamo anterior quedo huerfano: su corrida sigue abierta y nadie la va a
                // cerrar. Cerrarla ahora es lo que evita una fila RUNNING eterna que confunde a
                // quien mire la bitacora dentro de un mes.
                var abandoned = await _runs.AbandonOpenRunsAsync(
                    state.Code, nowUtc,
                    $"Reclamo vencido de '{claimed.PreviousClaimedBy}' retomado por '{_options.InstanceId}'.",
                    cancellationToken).ConfigureAwait(false);

                _logger.LogWarning(
                    "Reclamo vencido retomado en {TaskCode}: lo tenia {PreviousOwner} desde {PreviousClaimUtc}. Corridas cerradas: {AbandonedCount}.",
                    state.Code, claimed.PreviousClaimedBy, claimed.PreviousClaimedAtUtc, abandoned);
            }

            var runId = await _runs.StartAsync(new AutomatedTaskRunStart(
                state.Code, nowUtc, window.FromUtc, window.ToUtc, state.LastCutoffUtc,
                triggeredByUser, _options.InstanceId), cancellationToken).ConfigureAwait(false);

            var result = await ExecuteTaskBodyAsync(state.Code, window, cancellationToken).ConfigureAwait(false);

            // El cursor que propone la tarea se recorta aqui. No es desconfianza gratuita: es la
            // unica barrera que impide que un descuido dentro de una tarea salte trabajo sin
            // dejar rastro, y es un error que no produce ningun sintoma hasta que alguien nota
            // que faltan los datos de un dia entero.
            var cursorAfterUtc = ClampCursor(result.CursorAfterUtc, state.LastCutoffUtc, window.ToUtc);

            // Si la ventana venia recortada y el cursor llego a su final, todavia hay atraso: se
            // vuelve a agendar de inmediato para seguir alcanzando el presente por tramos.
            var stillBehind = cursorAfterUtc >= window.ToUtc && window.ToUtc < nowUtc;
            var nextRunAtUtc = stillBehind
                ? nowUtc
                : TaskScheduleCalculator.ComputeNextRunUtc(state.Schedule, nowUtc, _timeZone);

            var finishedAtUtc = _clock.GetUtcNow().UtcDateTime;
            var status = ToStorageStatus(result.Status);

            await _runs.FinishAsync(
                runId, status, result.ItemsProcessed, result.ItemsFailed, result.Message,
                finishedAtUtc, cursorAfterUtc, cancellationToken).ConfigureAwait(false);

            await _tasks.CompleteRunAsync(
                state.Code, nowUtc, nextRunAtUtc, cursorAfterUtc, status, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Tarea {TaskCode}: {Status} | ventana {WindowFromUtc} .. {WindowToUtc} | cursor {CursorBeforeUtc} -> {CursorAfterUtc} | {ItemsProcessed} procesados, {ItemsFailed} fallidos.",
                state.Code, status, window.FromUtc, window.ToUtc, state.LastCutoffUtc, cursorAfterUtc,
                result.ItemsProcessed, result.ItemsFailed);

            return new ExecutedRun(result with { CursorAfterUtc = cursorAfterUtc }, window);
        }
        finally
        {
            // SIEMPRE. Aunque cerrar la bitacora o mover el cursor hayan reventado, y aunque la
            // aplicacion se este apagando: por eso va con CancellationToken.None y no con el de
            // la peticion. Un reclamo que no se suelta deja la tarea congelada hasta que venza
            // por antiguedad, y eso son quince minutos de nada.
            await _tasks.ReleaseClaimAsync(state.Code, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task<TaskRunResult> ExecuteTaskBodyAsync(
        string code, TaskWindow window, CancellationToken cancellationToken)
    {
        if (!_implementations.TryGetValue(code, out var implementation))
        {
            // No deberia pasar: EnsureEveryCatalogTaskIsRegistered lo impide al arrancar. Si
            // llega aqui, se registra como fallo de la corrida en vez de tumbar el despachador
            // entero y con el las otras tareas.
            _logger.LogCritical("La tarea {TaskCode} no tiene implementacion registrada.", code);
            return TaskRunResult.Failed(window, $"La tarea '{code}' no tiene implementacion registrada.");
        }

        try
        {
            return await implementation.ExecuteAsync(window, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Una tarea que revienta no puede tumbar a las demas ni al despachador. Se anota
            // como FAILED, el cursor no se mueve, y el siguiente ciclo lo reintenta.
            _logger.LogError(exception, "La tarea {TaskCode} lanzo una excepcion.", code);
            return TaskRunResult.Failed(window, $"Excepcion no controlada: {exception.Message}");
        }
    }

    /// <summary>
    /// La ventana: <c>[cursor, ahora]</c>, recortada a un maximo.
    ///
    /// Recortarla NO pierde trabajo, y esa es la propiedad que hace valiosa toda esta forma de
    /// hacer las cosas: lo que queda por encima del cursor sigue ahi para la siguiente corrida.
    /// Con un rango calculado por reloj, el mismo recorte tiraria ese tramo al suelo.
    /// </summary>
    public static TaskWindow BuildWindow(DateTime cursorUtc, DateTime nowUtc, TimeSpan maxWindow)
    {
        if (nowUtc < cursorUtc) return new TaskWindow(cursorUtc, cursorUtc);

        var toUtc = nowUtc - cursorUtc > maxWindow ? cursorUtc + maxWindow : nowUtc;
        return new TaskWindow(cursorUtc, toUtc);
    }

    /// <summary>
    /// El cursor no retrocede y no se adelanta a lo que la ventana cubrio.
    ///
    /// Las dos direcciones importan, y por motivos distintos: retroceder reprocesa (molesto pero
    /// visible), adelantarse SALTA trabajo (silencioso y permanente).
    /// </summary>
    public static DateTime ClampCursor(DateTime proposedUtc, DateTime currentCursorUtc, DateTime windowToUtc)
    {
        if (proposedUtc < currentCursorUtc) return currentCursorUtc;
        if (proposedUtc > windowToUtc) return windowToUtc;
        return proposedUtc;
    }

    public static string ToStorageStatus(TaskRunStatus status) => status switch
    {
        TaskRunStatus.Success => "SUCCESS",
        TaskRunStatus.Partial => "PARTIAL",
        TaskRunStatus.Failed => "FAILED",
        TaskRunStatus.Skipped => "SKIPPED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Estado de corrida desconocido.")
    };
}
