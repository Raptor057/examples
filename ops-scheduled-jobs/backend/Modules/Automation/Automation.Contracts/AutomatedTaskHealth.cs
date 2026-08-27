namespace Automation.Contracts;

/// <summary>
/// La UNICA via por la que otro modulo puede preguntar por el estado de las tareas.
///
/// Es un POCO a proposito: sin dependencias, sin entidades de dominio y sin tipos de
/// Application. Un modulo que necesite saber si el resumen diario esta al dia consume esto, no
/// el repositorio ni la tabla. Asi el modulo de automatizacion puede cambiar por dentro sin
/// arrastrar a nadie.
///
/// Con un solo modulo esta carpeta parece de sobra, y lo es hoy. Existe para que el segundo
/// modulo no obligue a inventar la frontera con prisa.
/// </summary>
public sealed class AutomatedTaskHealth
{
    public string Code { get; init; } = string.Empty;

    public bool IsEnabled { get; init; }

    /// <summary>Hasta donde se proceso bien. Es lo que de verdad le interesa a otro modulo.</summary>
    public DateTime LastCutoffUtc { get; init; }

    public DateTime? LastRunAtUtc { get; init; }

    public string? LastStatus { get; init; }
}
