namespace Automation.Domain.Tasks;

/// <summary>
/// El tope de elementos que una corrida acepta traer.
///
/// Es un cortacircuitos, no una paginacion. Lo que acota el tamano de una corrida es la LONGITUD
/// DE LA VENTANA (la recorta el despachador y sigue avanzando el cursor por tramos hasta
/// alcanzar el presente); esto solo existe para que un volumen absurdo dentro de una ventana
/// normal no se procese a medias en silencio.
///
/// Truncar por numero de filas seria peor que no acotar: la fila numero 5001 y la 5000 pueden
/// compartir el mismo milisegundo, y cortar ahi deja al cursor sin un limite limpio donde
/// pararse. Por eso, si se alcanza el tope, la corrida termina en FALLO y el cursor NO se mueve:
/// alguien mira lo que paso en vez de descubrir el hueco tres meses despues.
/// </summary>
public static class TaskBatchLimits
{
    public const int MaxItemsPerRun = 5000;
}
