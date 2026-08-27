namespace Automation.Domain.Tasks;

/// <summary>
/// Como le fue a una corrida.
///
/// SKIPPED y la ausencia de fila en la bitacora NO son lo mismo: el primero dice "desperto y no
/// habia nada que hacer" y el segundo dice "no desperto". El segundo es una falla, y si los dos
/// se ven igual en pantalla, nadie la detecta.
/// </summary>
public enum TaskRunStatus
{
    Success,
    Partial,
    Failed,
    Skipped
}

/// <summary>
/// La ventana que cubre una corrida: <c>[FromUtc, ToUtc]</c>, donde <c>FromUtc</c> es el CURSOR
/// y no una hora calculada con el reloj.
///
/// La diferencia con "hoy de 07:00 a 19:00" no es de estilo. Por horario, lo que ocurra fuera
/// de la franja no se procesa nunca y cada corrida termina en verde. Por cursor, si la tarea no
/// corrio ayer, hoy se lleva dos dias y no se pierde nada.
/// </summary>
public sealed record TaskWindow
{
    public TaskWindow(DateTime fromUtc, DateTime toUtc)
    {
        if (fromUtc.Kind != DateTimeKind.Utc || toUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La ventana de una tarea viaja SIEMPRE en UTC.", nameof(fromUtc));

        if (toUtc < fromUtc)
            throw new ArgumentException("El fin de la ventana no puede ser anterior al inicio.", nameof(toUtc));

        FromUtc = fromUtc;
        ToUtc = toUtc;
    }

    public DateTime FromUtc { get; }

    public DateTime ToUtc { get; }

    public TimeSpan Length => ToUtc - FromUtc;
}

/// <summary>
/// Lo que devuelve una tarea. <see cref="CursorAfterUtc"/> es el campo importante: dice hasta
/// donde salio TODO bien, y es lo unico que el despachador guarda como cursor nuevo.
///
/// Una tarea que devuelve <c>ToUtc</c> habiendo fallado un elemento hace desaparecer ese
/// elemento para siempre, sin error visible.
/// </summary>
public sealed record TaskRunResult(
    TaskRunStatus Status,
    int ItemsProcessed,
    int ItemsFailed,
    string Message,
    DateTime CursorAfterUtc)
{
    /// <summary>Todo salio bien: el cursor llega al final de la ventana.</summary>
    public static TaskRunResult Success(TaskWindow window, int itemsProcessed, string message) =>
        new(TaskRunStatus.Success, itemsProcessed, 0, message, window.ToUtc);

    /// <summary>
    /// No habia nada que hacer. El cursor SI avanza: una ventana vacia esta completamente
    /// procesada, y no avanzarlo obligaria a releerla eternamente.
    /// </summary>
    public static TaskRunResult Skipped(TaskWindow window, string message) =>
        new(TaskRunStatus.Skipped, 0, 0, message, window.ToUtc);

    /// <summary>
    /// Fallo parcial: los buenos se procesaron y los malos se contaron. El cursor se queda en
    /// <paramref name="cursorAfterUtc"/>, que la tarea calcula como el instante hasta el cual
    /// no quedo nada sin resolver.
    /// </summary>
    public static TaskRunResult Partial(int itemsProcessed, int itemsFailed, string message, DateTime cursorAfterUtc) =>
        new(TaskRunStatus.Partial, itemsProcessed, itemsFailed, message, cursorAfterUtc);

    /// <summary>La corrida se cayo entera: el cursor se queda donde estaba.</summary>
    public static TaskRunResult Failed(TaskWindow window, string message, int itemsProcessed = 0, int itemsFailed = 0) =>
        new(TaskRunStatus.Failed, itemsProcessed, itemsFailed, message, window.FromUtc);
}

/// <summary>
/// El contrato minimo que implementa cada tarea. Es todo lo que el despachador necesita saber.
///
/// Agregar una tarea es escribir una clase mas y registrarla; el despachador no se toca. Si
/// para meter una tarea hubiera que editar el despachador, en tres tareas seria un switch y en
/// diez, el archivo que nadie quiere abrir.
/// </summary>
public interface IAutomatedTask
{
    /// <summary>El mismo Code que declara el catalogo y que guarda la fila de configuracion.</summary>
    string Code { get; }

    Task<TaskRunResult> ExecuteAsync(TaskWindow window, CancellationToken cancellationToken);
}
