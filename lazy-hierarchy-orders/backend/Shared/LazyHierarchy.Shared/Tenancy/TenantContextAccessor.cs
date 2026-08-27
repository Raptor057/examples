using LazyHierarchy.Kernel;

namespace LazyHierarchy.Shared.Tenancy;

/// <summary>
/// Accessor del tenant vigente. El estado va en AsyncLocal, asi que vive ligado al flujo de
/// la peticion aunque el accessor sea singleton, y la capa de datos no necesita conocer ASP.NET.
/// </summary>
public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<TenantContext?> Value = new();

    public TenantContext? Current
    {
        get => Value.Value;
        set => Value.Value = value;
    }
}
