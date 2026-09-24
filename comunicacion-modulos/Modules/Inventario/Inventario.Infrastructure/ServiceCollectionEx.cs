using Inventario.Domain.Repositories;
using Inventario.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Inventario.Infrastructure;

// Registro DI de la capa Infrastructure: enlaza el puerto (IProductoRepository)
// con su adaptador concreto (InMemoryProductoRepository).
public static class ServiceCollectionEx
{
    public static IServiceCollection AddInventarioInfrastructureServices(this IServiceCollection services)
    {
        // Singleton solo para que el stock sobreviva entre requests en la demo.
        services.AddSingleton<IProductoRepository, InMemoryProductoRepository>();
        return services;
    }
}
