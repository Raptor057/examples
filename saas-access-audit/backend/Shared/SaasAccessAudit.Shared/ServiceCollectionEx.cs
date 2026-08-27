using System.Reflection;
using SaasAccessAudit.Kernel;
using SaasAccessAudit.Shared.Identity;
using SaasAccessAudit.Shared.Persistence;
using SaasAccessAudit.Shared.Postgres;
using SaasAccessAudit.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SaasAccessAudit.Shared;

public static class ServiceCollectionEx
{
    /// <summary>
    /// Nombre del ensamblado donde viven las migraciones. Con UN solo DbContext hay UNA sola
    /// historia de migraciones, asi que no puede vivir dentro de un modulo: elegir el modulo
    /// Access lo volveria especial y el dia que Audit necesite una columna tendria que pedirsela.
    /// Vive en el Host, que es quien ya conoce a todos los modulos.
    /// </summary>
    public const string MigrationsAssembly = "SaasAccessAudit.Host";

    /// <summary>
    /// Registro explicito de la capa de datos compartida. Ni escaneo de ensamblados ni
    /// convenciones magicas: un registro que falta tiene que doler en la primera llamada.
    /// </summary>
    public static IServiceCollection AddSharedPostgresInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] modelConfigurationAssemblies)
    {
        // Las columnas del esquema van en snake_case y las propiedades de los Row en PascalCase.
        // Esta bandera global es el puente, y evita entrecomillar alias en el SQL.
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        // Dapper: factory y conexion tipados por marcador (open generics).
        services.AddScoped(typeof(ConfigurationSqlDbConnectionFactory<>));
        services.AddScoped(typeof(ConfigurationSqlDbConnection<>));

        // Contexto de la peticion: un solo accessor de tenant para EF y para Dapper, y un solo
        // accessor de identidad para el control de acceso y para la bitacora. Que la bitacora
        // lea de la MISMA fuente que la autorizacion es lo que garantiza que el renglon diga
        // quien actuo de verdad y no quien dijo el cliente que era.
        services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
        services.AddSingleton<IUserContextAccessor, UserContextAccessor>();
        services.AddScoped<TenantScope>();

        // EF Core: DbContext unico + interceptores.
        services.AddSingleton(new ModelConfigurationSources(modelConfigurationAssemblies));
        services.AddSingleton<AuditSoftDeleteInterceptor>();
        services.AddSingleton<SqlTimingInterceptor>();
        services.AddDbContext<AppDbContext>((provider, options) =>
            options.UseNpgsql(
                    configuration.GetConnectionString("MainDb"),
                    npgsql => npgsql
                        .EnableRetryOnFailure()
                        .MigrationsAssembly(MigrationsAssembly))
                .AddInterceptors(
                    provider.GetRequiredService<AuditSoftDeleteInterceptor>(),
                    provider.GetRequiredService<SqlTimingInterceptor>()));

        return services;
    }
}
