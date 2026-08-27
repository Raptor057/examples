using Microsoft.Extensions.DependencyInjection;
using OpsJobs.Shared.Migrations;
using OpsJobs.Shared.SqlServer;

namespace OpsJobs.Shared;

public static class ServiceCollectionEx
{
    /// <summary>
    /// Registro explicito de la capa de datos compartida. Ni escaneo de ensamblados ni
    /// convenciones magicas: un registro que falta tiene que doler en la primera llamada
    /// (regla de oro 5).
    /// </summary>
    public static IServiceCollection AddSharedSqlServerInfrastructure(this IServiceCollection services)
    {
        // El esquema va en PascalCase y las propiedades de los Row tambien, asi que aqui NO se
        // activa MatchNamesWithUnderscores: en SQL Server el nombre de la columna y el de la
        // propiedad coinciden tal cual.
        services.AddScoped(typeof(ConfigurationSqlDbConnectionFactory<>));
        services.AddScoped(typeof(ConfigurationSqlDbConnection<>));
        services.AddScoped<SqlScriptMigrator>();
        return services;
    }
}
