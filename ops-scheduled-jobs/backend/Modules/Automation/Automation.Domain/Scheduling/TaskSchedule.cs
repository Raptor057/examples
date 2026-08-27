using System.Globalization;

namespace Automation.Domain.Scheduling;

/// <summary>Como se decide cuando despierta una tarea.</summary>
public enum ScheduleKind
{
    /// <summary>Cada N minutos desde la ultima corrida.</summary>
    Interval,

    /// <summary>A estas horas LOCALES, todos los dias.</summary>
    DailyAt
}

/// <summary>
/// El horario de una tarea. Decide CUANDO despierta, nunca QUE incluye: eso lo decide el cursor.
/// Mezclar las dos cosas es el antipatron que hace que un cambio de turno tire trabajo al suelo
/// sin que ninguna corrida salga en rojo.
/// </summary>
public sealed record TaskSchedule
{
    public const string IntervalCode = "INTERVAL";
    public const string DailyAtCode = "DAILY_AT";

    private TaskSchedule(ScheduleKind kind, int? intervalMinutes, string? runAtLocalTimes)
    {
        Kind = kind;
        IntervalMinutes = intervalMinutes;
        RunAtLocalTimes = runAtLocalTimes;
    }

    public ScheduleKind Kind { get; }

    public int? IntervalMinutes { get; }

    /// <summary>
    /// Horas locales separadas por coma, en formato HH:mm. Se capturan en hora LOCAL porque es
    /// como las piensa quien opera: "el corte de las 2 de la manana". La conversion a UTC
    /// ocurre en un solo lugar (<see cref="SchedulingTimeZone"/>).
    /// </summary>
    public string? RunAtLocalTimes { get; }

    public static TaskSchedule EveryMinutes(int minutes)
    {
        if (minutes <= 0) throw new ArgumentOutOfRangeException(nameof(minutes), "El intervalo tiene que ser mayor que cero.");
        return new TaskSchedule(ScheduleKind.Interval, minutes, null);
    }

    public static TaskSchedule DailyAt(params TimeOnly[] localTimes)
    {
        if (localTimes is null || localTimes.Length == 0)
            throw new ArgumentException("Un horario diario necesita al menos una hora.", nameof(localTimes));

        var text = string.Join(',', localTimes.Order().Select(time => time.ToString("HH\\:mm", CultureInfo.InvariantCulture)));
        return new TaskSchedule(ScheduleKind.DailyAt, null, text);
    }

    /// <summary>Reconstruye el horario desde la fila de configuracion.</summary>
    public static TaskSchedule FromStorage(string scheduleKind, int? intervalMinutes, string? runAtLocalTimes) =>
        scheduleKind switch
        {
            IntervalCode => EveryMinutes(intervalMinutes ?? throw new ArgumentException(
                "Un horario INTERVAL necesita IntervalMinutes.", nameof(intervalMinutes))),
            DailyAtCode => new TaskSchedule(ScheduleKind.DailyAt, null, runAtLocalTimes ?? throw new ArgumentException(
                "Un horario DAILY_AT necesita RunAtLocalTimes.", nameof(runAtLocalTimes))),
            _ => throw new ArgumentOutOfRangeException(nameof(scheduleKind), scheduleKind, "Tipo de horario desconocido.")
        };

    public string StorageKind => Kind == ScheduleKind.Interval ? IntervalCode : DailyAtCode;

    /// <summary>
    /// Las horas locales ya parseadas y ordenadas. Una hora mal escrita revienta aqui, al leer
    /// la configuracion, y no dentro del despachador a las 3 de la manana.
    /// </summary>
    public IReadOnlyList<TimeOnly> ParseLocalTimes()
    {
        if (Kind != ScheduleKind.DailyAt || string.IsNullOrWhiteSpace(RunAtLocalTimes)) return [];

        var times = new List<TimeOnly>();
        foreach (var piece in RunAtLocalTimes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TimeOnly.TryParseExact(piece, "HH\\:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                throw new FormatException($"'{piece}' no es una hora local valida. El formato es HH:mm.");

            times.Add(time);
        }

        times.Sort();
        return times;
    }
}
