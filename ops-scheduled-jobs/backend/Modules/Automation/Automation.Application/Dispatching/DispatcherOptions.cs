namespace Automation.Application.Dispatching;

/// <summary>
/// Los cuatro numeros que gobiernan al despachador. Van juntos porque solo tienen sentido
/// juntos: cambiar uno sin mirar los otros produce comportamientos raros y dificiles de atar a
/// su causa.
/// </summary>
public sealed record DispatcherOptions
{
    /// <summary>
    /// Quien reclama. Debe identificar a la REPLICA, no al servicio: con dos instancias del
    /// mismo contenedor, un identificador compartido hace imposible saber cual se murio.
    /// </summary>
    public required string InstanceId { get; init; }

    /// <summary>Cada cuanto despierta el despachador a preguntar si hay tareas vencidas.</summary>
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Cuanto puede durar un reclamo antes de considerarse abandonado.
    ///
    /// Es el numero mas delicado del archivo. Demasiado corto, y una tarea lenta pero viva se
    /// queda sin su reclamo y otra instancia la ejecuta en paralelo. Demasiado largo, y una
    /// instancia que murio deja su tarea congelada ese rato. Tiene que ser comodamente mayor que
    /// la corrida mas larga que exista.
    /// </summary>
    public TimeSpan StaleClaimAfter { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Cuanto pasado abarca UNA corrida como maximo.
    ///
    /// Sin esto, una tarea que estuvo pausada un mes despierta con una ventana de un mes y
    /// procesa treinta dias de golpe. Con esto, avanza por tramos: cada corrida cierra su tramo,
    /// mueve el cursor y -mientras siga habiendo atraso- el despachador la vuelve a agendar de
    /// inmediato en vez de esperar al siguiente horario.
    ///
    /// Lo importante es que recortar la ventana NO pierde nada: lo que queda por encima del
    /// cursor entra en la siguiente corrida. Ese es justo el poder de tomar la ventana del
    /// cursor y no del reloj.
    /// </summary>
    public TimeSpan MaxWindow { get; init; } = TimeSpan.FromDays(2);
}
