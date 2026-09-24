namespace Printing.Domain.Entities;

/// <summary>
/// Una plantilla ZPL guardada. La llave de negocio es <c>(Code, Dpi)</c>, no el codigo solo:
/// las coordenadas del ZPL van en PUNTOS y no escalan solas, asi que la misma etiqueta necesita
/// una version dibujada para 203 dpi y otra para 300. Ver ADR-0003.
/// </summary>
public sealed record LabelTemplate
{
    public int Id { get; init; }

    /// <summary>Codigo de negocio, siempre en mayusculas. Ej. "BOX_LABEL".</summary>
    public required string Code { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    /// <summary>El cuerpo ZPL con sus marcadores <c>{{VARIABLE}}</c>.</summary>
    public required string Body { get; init; }

    public required int Dpi { get; init; }

    /// <summary>
    /// Version del cuerpo vigente. Arranca en 1 y sube en cada edicion. Se guarda en la bitacora
    /// de impresion para poder contestar con QUE etiqueta se imprimio algo hace tres meses.
    /// </summary>
    public int Version { get; init; } = 1;

    public bool IsActive { get; init; } = true;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}
