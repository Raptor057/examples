namespace Printing.Domain.Entities;

/// <summary>
/// Que paso con la peticion de impresion. Son tres desenlaces, no dos, y confundirlos es lo que
/// hace que alguien crea que una etiqueta salio cuando sigue esperando en la cola.
/// </summary>
public enum PrintDisposition
{
    /// <summary>Los bytes llegaron a la impresora.</summary>
    Printed = 0,

    /// <summary>La impresora no contesto y el trabajo quedo encolado para reintentarse.</summary>
    Queued = 1,

    /// <summary>Ni salio ni se encolo. El motivo va en el mensaje.</summary>
    Rejected = 2,
}
