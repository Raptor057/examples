using System.Reflection;
using LazyHierarchy.Kernel;
using LazyHierarchy.Shared.Persistence;
using LazyHierarchy.Shared.Postgres;
using LazyHierarchy.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LazyHierarchy.Shared;

public static class ServiceCollectionEx
{
    /// <summary>
    /// Registro explicito de la capa de datos compartida. Ni escaneo de ensamblados ni
    /// convenciones magicas: un registro que falta tiene que doler en la primera llamada.
    /// </summary>
    public static IServiceCollection AddSharedPostgresInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] modelConfigurationAssemblies)
    {
        // Las columnas del esquema van en snake_case y las propiedades de los Row en
        // PascalCase. Esta bandera global es el puente, y evita tener que entrecomillar alias
        // en el SQL solo para conservar mayusculas.
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        // Dapper: factory y conexion tipados por marcador (open generics).
        services.AddScoped(typeof(ConfigurationSqlDbConnectionFactory<>));
        services.AddScoped(typeof(ConfigurationSqlDbConnection<>));

        // Tenancy: un solo accessor para EF y para Dapper.
        services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
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
                        .MigrationsAssembly("Orders.Infrastructure"))
                .AddInterceptors(
                    provider.GetRequiredService<AuditSoftDeleteInterceptor>(),
                    provider.GetRequiredService<SqlTimingInterceptor>()));

        return services;
    }
}
