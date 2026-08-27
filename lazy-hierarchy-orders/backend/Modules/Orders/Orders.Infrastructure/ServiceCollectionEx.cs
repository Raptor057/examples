using Microsoft.Extensions.DependencyInjection;
using Orders.Domain.Repositories;
using Orders.Infrastructure.Repositories;
using Orders.Infrastructure.Seeding;

namespace Orders.Infrastructure;

public static class ServiceCollectionEx
{
    /// <summary>
    /// Registro explicito, a mano, uno por uno. Es mas verboso que un escaneo de ensamblados y
    /// es a proposito: un registro que falta es invisible para el compilador.
    /// </summary>
    public static IServiceCollection AddOrdersInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IOrdersTreeReadRepository, OrdersTreeReadRepository>();
        services.AddScoped<IOrderWriteRepository, OrderWriteRepository>();
        services.AddScoped<OrdersSeeder>();
        return services;
    }

    /// <summary>
    /// Ensamblado del que el DbContext unico toma las IEntityTypeConfiguration de este modulo.
    /// </summary>
    public static System.Reflection.Assembly ModelConfigurationAssembly =>
        typeof(ServiceCollectionEx).Assembly;
}
