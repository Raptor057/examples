namespace Automation.Domain.Scheduling;

/// <summary>
/// Calcula cuando despierta la tarea la proxima vez. Funcion pura: mismos argumentos, mismo
/// resultado, sin leer el reloj por su cuenta. Por eso se puede probar el cambio de horario de
/// verano sin cambiarle la hora a la maquina.
/// </summary>
public static class TaskScheduleCalculator
{
    /// <summary>Cuantos dias hacia adelante se buscan candidatos en un horario diario.</summary>
    private const int DailySearchHorizonDays = 3;

    public static DateTime ComputeNextRunUtc(TaskSchedule schedule, DateTime afterUtc, SchedulingTimeZone timeZone)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(timeZone);

        if (afterUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Se esperaba una fecha en UTC.", nameof(afterUtc));

        return schedule.Kind switch
        {
            ScheduleKind.Interval => afterUtc.AddMinutes(schedule.IntervalMinutes ?? 0),
            ScheduleKind.DailyAt => NextDailyRunUtc(schedule, afterUtc, timeZone),
            _ => throw new ArgumentOutOfRangeException(nameof(schedule), schedule.Kind, "Tipo de horario desconocido.")
        };
    }

    private static DateTime NextDailyRunUtc(TaskSchedule schedule, DateTime afterUtc, SchedulingTimeZone timeZone)
    {
        var localTimes = schedule.ParseLocalTimes();
        if (localTimes.Count == 0)
            throw new InvalidOperationException("Un horario diario sin horas configuradas no puede calcular su proxima corrida.");

        // Se parte del dia LOCAL, no del dia UTC: a las 23:00 hora local de Mexico ya es el dia
        // siguiente en UTC, y buscar candidatos sobre la fecha UTC se saltaria el corte de esa
        // misma noche.
        var localDay = DateOnly.FromDateTime(timeZone.ToLocal(afterUtc));

        // Se busca sobre varios dias porque el candidato mas cercano en hora local no siempre es
        // el mas cercano en UTC alrededor del cambio de horario.
        var candidates = new List<DateTime>();
        for (var dayOffset = 0; dayOffset <= DailySearchHorizonDays; dayOffset++)
        {
            var day = localDay.AddDays(dayOffset);
            foreach (var time in localTimes)
            {
                var candidateUtc = timeZone.ToUtc(day.ToDateTime(time));
                if (candidateUtc > afterUtc) candidates.Add(candidateUtc);
            }
        }

        if (candidates.Count == 0)
            throw new InvalidOperationException(
                $"No se encontro una proxima corrida en los siguientes {DailySearchHorizonDays} dias.");

        return candidates.Min();
    }
}
