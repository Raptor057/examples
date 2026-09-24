using Printing.Domain.Entities;

namespace Printing.Domain.Abstractions;

public interface ILabelTemplateRepository
{
    Task<IReadOnlyList<LabelTemplate>> ListAsync(int? dpi, bool? isActive, CancellationToken cancellationToken);

    /// <summary>Por su llave de negocio completa: el codigo solo no identifica una plantilla.</summary>
    Task<LabelTemplate?> FindAsync(string code, int dpi, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string code, int dpi, CancellationToken cancellationToken);

    Task<LabelTemplate> CreateAsync(LabelTemplate template, CancellationToken cancellationToken);

    /// <summary>Devuelve false si no habia nada que actualizar, para que el caso de uso responda 404 y no un exito mentiroso.</summary>
    Task<bool> UpdateAsync(LabelTemplate template, CancellationToken cancellationToken);

    /// <summary>Baja logica. Nunca se borra una plantilla: hay etiquetas impresas que la referencian.</summary>
    Task<bool> DeactivateAsync(string code, int dpi, CancellationToken cancellationToken);

    /// <summary>
    /// El historial de una plantilla, de la mas reciente a la mas vieja. La vigente NO sale aqui:
    /// esto es lo que ya fue reemplazado.
    /// </summary>
    Task<IReadOnlyList<LabelTemplateVersion>> HistoryAsync(string code, int dpi, CancellationToken cancellationToken);

    /// <summary>Una version concreta, para reimprimir exactamente lo que se imprimio entonces.</summary>
    Task<LabelTemplateVersion?> FindVersionAsync(string code, int dpi, int version, CancellationToken cancellationToken);
}
