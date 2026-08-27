using SaasAccessAudit.Kernel;

namespace SaasAccessAudit.Shared.Identity;

/// <summary>
/// Guarda la identidad de la peticion en curso. Es AsyncLocal por la misma razon que el de
/// tenant: el valor tiene que seguir al flujo asincrono de la peticion y no filtrarse a otra.
/// </summary>
public sealed class UserContextAccessor : IUserContextAccessor
{
    private static readonly AsyncLocal<UserContext?> Storage = new();

    public UserContext? Current
    {
        get => Storage.Value;
        set => Storage.Value = value;
    }
}
