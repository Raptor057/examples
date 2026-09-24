using LabelPrinting.Shared.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace LabelPrinting.Shared;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddSharedPersistence(this IServiceCollection services)
    {
        services.AddScoped<SqlDbConnection>();
        services.AddScoped<SchemaBootstrapper>();
        return services;
    }
}
