namespace SaasAccessAudit.Kernel;

/// <summary>
/// Chasis de entidad. Da el Id relacional interno (nunca viaja en la API) y el PublicId
/// publico. Las convenciones del DbContext se apoyan en esta clase base para poner el
/// default de UUID y el indice unico sin que ninguna configuracion lo repita.
/// </summary>
public abstract class BaseEntity
{
    public long Id { get; set; }

    public Guid PublicId { get; set; }
}
