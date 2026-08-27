using Automation.Domain.Scheduling;

namespace Automation.Domain.Tasks;

/// <summary>
/// Lo que el codigo declara de una tarea: su identidad y sus valores POR DEFECTO. Todo lo que
/// aparece aqui es lo que no puede cambiar sin desplegar; todo lo demas vive en la fila de
/// configuracion.
/// </summary>
/// <param name="Code">Identificador estable. Es la llave entre el codigo y la base.</param>
/// <param name="DefaultSchedule">Horario con el que nace la tarea la primera vez.</param>
/// <param name="DefaultBacklogDays">
/// Cuanto pasado abarca el cursor al darla de alta. No es cosmetico: si el cursor naciera en
/// "ahora", la primera corrida no veria nada de lo que ya existia y ese hueco no se recupera.
/// </param>
public sealed record AutomatedTaskDefinition(
    string Code,
    TaskSchedule DefaultSchedule,
    int DefaultBacklogDays);

/// <summary>
/// EL CATALOGO. Que tareas existen lo decide ESTE archivo, no la base de datos.
///
/// El reparto es el mismo que en un catalogo de permisos:
///
/// - **El codigo manda sobre que existe.** Una fila en ops.AutomatedTask con un Code que no
///   este aqui no aparece en la pantalla y el despachador no la reclama. Nadie puede "dar de
///   alta una tarea" con un INSERT: no habria clase que ejecutar, y una tarea que solo existe
///   en base es una fila que promete trabajo que nunca ocurre.
/// - **La base manda sobre como esta configurada.** Prendida o pausada, cada cuanto, y hasta
///   donde llego. Eso se cambia a las 3 de la manana sin compilar nada.
///
/// El seeder da de alta las que falten y NO toca las que ya estan: si pisara la configuracion
/// existente, cada despliegue reanudaria las tareas que alguien habia pausado a proposito.
/// </summary>
public static class AutomatedTaskCatalog
{
    /// <summary>Recalcula el resumen diario de pedidos por canal.</summary>
    public const string RefreshOrderSummary = "refresh-order-summary";

    /// <summary>Da de baja los temporales que ya expiraron.</summary>
    public const string PurgeExpiredTempFiles = "purge-expired-temp-files";

    /// <summary>Reintenta los envios que quedaron sin entregar.</summary>
    public const string RetryPendingDispatches = "retry-pending-dispatches";

    public static readonly IReadOnlyList<AutomatedTaskDefinition> All =
    [
        // Intervalo corto a proposito: en un ejemplo hace falta poder ver el despachador
        // despachar sin esperar a la madrugada. En produccion serian 15 o 30 minutos.
        new AutomatedTaskDefinition(
            RefreshOrderSummary,
            TaskSchedule.EveryMinutes(5),
            DefaultBacklogDays: 10),

        // Horario diario, en hora LOCAL: es como lo pediria quien opera, y es el caso que
        // obliga a que la conversion a UTC viva en un solo sitio.
        new AutomatedTaskDefinition(
            PurgeExpiredTempFiles,
            TaskSchedule.DailyAt(new TimeOnly(2, 0), new TimeOnly(14, 0)),
            DefaultBacklogDays: 10),

        new AutomatedTaskDefinition(
            RetryPendingDispatches,
            TaskSchedule.EveryMinutes(3),
            DefaultBacklogDays: 10)
    ];

    private static readonly Dictionary<string, AutomatedTaskDefinition> ByCode =
        All.ToDictionary(definition => definition.Code, StringComparer.Ordinal);

    public static IReadOnlyCollection<string> Codes { get; } = All.Select(definition => definition.Code).ToArray();

    public static bool Contains(string code) => ByCode.ContainsKey(code);

    public static AutomatedTaskDefinition? Find(string code) =>
        ByCode.TryGetValue(code, out var definition) ? definition : null;
}
