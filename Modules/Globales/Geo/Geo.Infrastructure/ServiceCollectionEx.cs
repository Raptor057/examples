using Geo.Domain.Repositories;
using Geo.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Geo.Infrastructure;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddGeoInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IPaisRepository, InMemoryPaisRepository>();
        return services;
    }
}
