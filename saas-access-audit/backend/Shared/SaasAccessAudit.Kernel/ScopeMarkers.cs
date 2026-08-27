namespace SaasAccessAudit.Kernel;

/// <summary>
/// Ambito de aislamiento: la entidad pertenece a un tenant. Declarar esta interfaz es lo que
/// hace que el DbContext le aplique el global query filter; no hay un HasQueryFilter por
/// entidad que alguien pueda olvidar.
/// </summary>
public interface ITenantOwned
{
    long TenantId { get; set; }
}

/// <summary>Borrado logico. El interceptor convierte el Remove en un update.</summary>
public interface ISoftDeletable
{
    bool IsActive { get; set; }

    DateTime? DeletedAtUtc { get; set; }
}

/// <summary>Auditoria en UTC. Las fechas las pone el interceptor, nunca el handler.</summary>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }

    DateTime UpdatedAtUtc { get; set; }
}
