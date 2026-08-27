using Microsoft.Extensions.Options;
using SaasAccessAudit.Kernel;

namespace Access.Infrastructure.Authorization;

/// <summary>Seccion AccessControl de la configuracion.</summary>
public sealed class AccessControlOptions
{
    public const string SectionName = "AccessControl";

    /// <summary>
    /// Por defecto ENCENDIDO. Un default apagado convierte un olvido de configuracion en una
    /// aplicacion sin control de acceso, y eso no se nota mirando la pantalla: se nota cuando
    /// alguien hace algo que no debia.
    /// </summary>
    public bool EnforcePermissions { get; set; } = true;
}

/// <summary>
/// Lee el flag con IOptionsMonitor y no con IOptions, para que el valor se recargue cuando cambia
/// el archivo de configuracion. Asi el paseo del README -apagarlo, ver que se habria bloqueado,
/// encenderlo- no obliga a reiniciar el servicio a media demostracion.
/// </summary>
internal sealed class AccessControlSettings(IOptionsMonitor<AccessControlOptions> options) : IAccessControlSettings
{
    public bool EnforcePermissions => options.CurrentValue.EnforcePermissions;
}
