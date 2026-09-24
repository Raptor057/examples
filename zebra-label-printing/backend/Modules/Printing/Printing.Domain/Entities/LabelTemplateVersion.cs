namespace Printing.Domain.Entities;

/// <summary>
/// Una version anterior del cuerpo de una plantilla.
///
/// Existe para poder contestar "con que etiqueta se imprimio esto en julio". Sin historial, la
/// bitacora dice que se uso BOX_LABEL, pero BOX_LABEL de hoy puede no parecerse en nada a la de
/// entonces, y la pregunta se queda sin respuesta justo cuando importa: en una queja de cliente.
/// </summary>
public sealed record LabelTemplateVersion
{
    public long Id { get; init; }
    public required string Code { get; init; }
    public required int Dpi { get; init; }

    /// <summary>Numero de version de ESTE cuerpo. La primera alta es la 1.</summary>
    public required int Version { get; init; }

    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Body { get; init; }

    /// <summary>Cuando dejo de ser la vigente. Nulo en la fila que sigue vigente.</summary>
    public DateTime? ReplacedAtUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
