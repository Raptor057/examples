using Microsoft.Extensions.DependencyInjection;
using Ventas.Domain.Repositories;
using Ventas.Infrastructure.Repositories;

namespace Ventas.Infrastructure;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddVentasInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IVentaRepository, InMemoryVentaRepository>();
        return services;
    }
}
