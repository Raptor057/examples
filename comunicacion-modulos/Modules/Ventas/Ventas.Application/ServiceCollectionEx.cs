using Microsoft.Extensions.DependencyInjection;

namespace Ventas.Application;

public static class ServiceCollectionEx
{
    // Los handlers (IInteractor) los descubre y registra AddMediator escaneando
    // este assembly. Aqui irian servicios extra de aplicacion (p.ej. validators
    // FluentValidation). En este modulo no hay, asi que es un no-op.
    public static IServiceCollection AddVentasApplicationServices(this IServiceCollection services)
    {
        return services;
    }
}
