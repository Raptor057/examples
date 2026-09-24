using Inventario.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Inventario.Application;

// Registro DI de la capa Application del modulo. Aqui se publica la
// implementacion del contrato: a partir de este punto, cualquier modulo puede
// inyectar IConsultaInventario y el contenedor resuelve ConsultaInventarioService.
public static class ServiceCollectionEx
{
    public static IServiceCollection AddInventarioApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IConsultaInventario, ConsultaInventarioService>();
        return services;
    }
}
