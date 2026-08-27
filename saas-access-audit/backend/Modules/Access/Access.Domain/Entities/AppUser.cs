using SaasAccessAudit.Kernel;

namespace Access.Domain.Entities;

/// <summary>
/// Usuario administrado desde la consola.
///
/// NO declara ISoftDeletable, y esto SI merece explicacion porque parece un olvido: desactivar
/// un usuario es un ESTADO DE NEGOCIO que la pantalla tiene que seguir mostrando -en solo
/// lectura, junto a los activos-, no un borrado logico que deba desaparecer de las lecturas. Con
/// el filtro automatico, la fila se esconderia justo en el momento en que alguien quiere
/// comprobar que se desactivo y con que motivo. El aislamiento por tenant si aplica, y ese lo
/// pone ITenantOwned.
///
/// El MOTIVO de la desactivacion no vive aqui: vive en la bitacora. La fila guarda el estado
/// actual; la bitacora guarda como se llego a el, y solo ella sobrevive a que alguien reactive
/// al usuario.
/// </summary>
public sealed class AppUser : BaseEntity, ITenantOwned, IAuditable
{
    public long TenantId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? DeactivatedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
