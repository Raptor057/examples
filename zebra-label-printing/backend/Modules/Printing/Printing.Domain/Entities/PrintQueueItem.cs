namespace Printing.Domain.Entities;

/// <summary>En que va un trabajo encolado.</summary>
public enum PrintQueueStatus
{
    /// <summary>Esperando su turno o su siguiente reintento.</summary>
    Pending = 0,

    /// <summary>Salio. Se conserva un rato por trazabilidad y despues se purga.</summary>
    Sent = 1,

    /// <summary>Se agotaron los reintentos. NO se vuelve a intentar solo: alguien tiene que mirarlo.</summary>
    Dead = 2,

    /// <summary>Alguien lo cancelo a mano.</summary>
    Cancelled = 3,
}

/// <summary>
/// Un trabajo de impresion esperando a que la impresora conteste.
///
/// Guarda el ZPL YA RENDERIZADO, no la plantilla y los valores. Es deliberado: si la plantilla
/// cambia mientras el trabajo espera en la cola, la etiqueta que salga tiene que ser la que se
/// pidio, no la que la plantilla diga media hora despues.
/// </summary>
public sealed record PrintQueueItem
{
    public long Id { get; init; }

    /// <summary>El destino serializado. La cola no interpreta: lo guarda y se lo devuelve al adaptador.</summary>
    public required string TargetJson { get; init; }

    /// <summary>Como se describe el destino en un mensaje. Se guarda aparte para no deserializar al listar.</summary>
    public required string TargetLabel { get; init; }

    public string? TemplateCode { get; init; }

    public int? Dpi { get; init; }

    public required string Zpl { get; init; }

    public PrintQueueStatus Status { get; init; } = PrintQueueStatus.Pending;

    /// <summary>Cuantas veces se intento ya. Empieza en cero.</summary>
    public int Attempts { get; init; }

    /// <summary>Cuando toca el siguiente intento. La cola solo toma los que ya vencieron.</summary>
    public DateTime NextAttemptAtUtc { get; init; }

    public string? LastError { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }
}
