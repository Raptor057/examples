namespace Automation.Domain.Scheduling;

/// <summary>
/// EL UNICO LUGAR del proyecto donde hora local y UTC se convierten una en otra.
///
/// El reparto es este y no admite excepciones:
///
/// - El HORARIO se captura en hora local, porque asi lo piensa quien opera ("el corte de las 2
///   de la manana", no "las 08:00Z").
/// - TODAS las columnas de tiempo del esquema van en UTC, sin excepcion.
/// - El frontend convierte a la hora de quien mira, y es la unica capa que lo hace ahi.
///
/// Cuando esta conversion esta desperdigada, el sintoma no es un error: es una tarea que corre
/// una hora tarde media parte del ano y a nadie se le ocurre relacionarlo con el cambio de
/// horario de verano.
/// </summary>
public sealed class SchedulingTimeZone
{
    public SchedulingTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("La zona horaria de programacion es obligatoria.", nameof(timeZoneId));

        Id = timeZoneId;
        // Revienta al arrancar si el identificador no existe en la maquina. Vale mas eso que
        // descubrirlo cuando la primera tarea intente calcular su proxima corrida.
        Zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }

    public string Id { get; }

    public TimeZoneInfo Zone { get; }

    public DateTime ToLocal(DateTime utc)
    {
        if (utc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Se esperaba una fecha en UTC.", nameof(utc));

        return TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);
    }

    /// <summary>
    /// Convierte una hora local (sin zona) a UTC resolviendo los dos casos que el horario de
    /// verano crea y que la conversion ingenua ignora:
    ///
    /// - **Hora inexistente** (el reloj salta de 2:00 a 3:00): esa hora local no ocurre. Se
    ///   toma el primer instante que si existe. Si no, la tarea de las 2:00 simplemente no
    ///   correria ese dia.
    /// - **Hora repetida** (el reloj vuelve de 2:00 a 1:00): esa hora local ocurre dos veces.
    ///   Se toma la PRIMERA, para no retrasar la corrida una hora. Cualquiera de las dos es
    ///   defendible; lo que no lo es, es que la eleccion dependa de la version de la libreria.
    /// </summary>
    public DateTime ToUtc(DateTime local)
    {
        if (local.Kind == DateTimeKind.Utc)
            throw new ArgumentException("Se esperaba una hora local, no UTC.", nameof(local));

        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);

        if (Zone.IsInvalidTime(unspecified))
        {
            // El salto siempre es hacia adelante; una hora despues ya es una hora que existe.
            var shifted = unspecified.AddHours(1);
            while (Zone.IsInvalidTime(shifted)) shifted = shifted.AddMinutes(15);
            return TimeZoneInfo.ConvertTimeToUtc(shifted, Zone);
        }

        if (Zone.IsAmbiguousTime(unspecified))
        {
            // GetAmbiguousTimeOffsets devuelve los dos desfases posibles; el mayor corresponde
            // al primero de los dos instantes (todavia en horario de verano).
            var offsets = Zone.GetAmbiguousTimeOffsets(unspecified);
            var earliest = offsets.Max();
            return new DateTimeOffset(unspecified, earliest).UtcDateTime;
        }

        return TimeZoneInfo.ConvertTimeToUtc(unspecified, Zone);
    }
}
