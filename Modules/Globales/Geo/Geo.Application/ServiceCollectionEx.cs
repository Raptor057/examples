using Geo.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Geo.Application;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddGeoApplicationServices(this IServiceCollection services)
    {
        // Publica el contrato cross-module del global Geo.
        services.AddScoped<IGeoCatalog, GeoCatalogService>();
        return services;
    }
}
