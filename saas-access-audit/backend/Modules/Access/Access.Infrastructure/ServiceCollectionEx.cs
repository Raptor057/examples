using System.Reflection;
using Access.Domain.Repositories;
using Access.Infrastructure.Authorization;
using Access.Infrastructure.Identity;
using Access.Infrastructure.Repositories;
using Access.Infrastructure.Seeding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SaasAccessAudit.Kernel;

namespace Access.Infrastructure;

public static class ServiceCollectionEx
{
    /// <summary>
    /// Registro explicito, a mano, uno por uno. Es mas verboso que un escaneo de ensamblados y es
    /// a proposito: un registro que falta es invisible para el compilador.
    /// </summary>
    public static IServiceCollection AddAccessInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAccessReadRepository, AccessReadRepository>();
        services.AddScoped<IAccessWriteRepository, AccessWriteRepository>();

        // El evaluador es SCOPED porque cachea los permisos efectivos durante la peticion. Como
        // singleton serviria permisos de una persona a otra; como transient los recalcularia en
        // cada endpoint de la misma llamada.
        services.AddScoped<IPermissionEvaluator, PermissionEvaluator>();

        services.AddScoped<IIdentityProviderGateway, FakeIdentityProviderGateway>();

        services.Configure<AccessControlOptions>(configuration.GetSection(AccessControlOptions.SectionName));
        services.AddSingleton<IAccessControlSettings, AccessControlSettings>();

        services.AddScoped<PermissionCatalogSeeder>();

        return services;
    }

    /// <summary>Ensamblado del que el DbContext unico toma las configuraciones de este modulo.</summary>
    public static Assembly ModelConfigurationAssembly => typeof(ServiceCollectionEx).Assembly;
}
