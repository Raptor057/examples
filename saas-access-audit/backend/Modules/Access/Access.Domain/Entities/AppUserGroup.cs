using SaasAccessAudit.Kernel;

namespace Access.Domain.Entities;

/// <summary>
/// Pertenencias de un usuario.
///
/// En un SaaS real esta tabla es un ESPEJO del directorio del proveedor de identidad: la verdad
/// vive alla y llega firmada en el token. Aqui existe por dos motivos concretos: la pantalla de
/// usuarios muestra a que roles llega cada persona, y el emisor de tokens del ejemplo necesita
/// de donde sacar los grupos al emitir.
///
/// Lo que NO hace es decidir permisos en tiempo de peticion: eso lo deciden los grupos del token.
/// </summary>
public sealed class AppUserGroup : BaseEntity, ITenantOwned, ISoftDeletable, IAuditable
{
    public long TenantId { get; set; }

    public long UserId { get; set; }

    public string GroupName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? DeletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
