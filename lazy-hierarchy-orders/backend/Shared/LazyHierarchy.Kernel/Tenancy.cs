namespace LazyHierarchy.Kernel;

/// <summary>Tenant vigente para la peticion en curso.</summary>
public sealed record TenantContext(long TenantId);

/// <summary>
/// Unica fuente del tenant actual. La llena el middleware a partir del claim del token;
/// nunca se alimenta de la query string ni del cuerpo de la peticion.
/// </summary>
public interface ITenantContextAccessor
{
    TenantContext? Current { get; set; }
}

public static class SystemContext
{
    /// <summary>
    /// Contexto sistema: no es "ver todo", es "no soy ningun tenant". Sin claims (migraciones,
    /// seeder, jobs de arranque) los filtros comparan contra este valor y no devuelven filas
    /// de negocio: fail-closed por diseno.
    /// </summary>
    public const long TenantId = 0L;
}
