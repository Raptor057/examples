using System.Reflection;
using Audit.Contracts;
using Audit.Domain.Repositories;
using Audit.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Infrastructure;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddAuditInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IAuditReadRepository, AuditReadRepository>();
        services.AddScoped<IAuditWriteRepository, AuditWriteRepository>();

        // Las dos puertas por las que otros modulos escriben en la bitacora. Si falta una de
        // estas lineas el proyecto compila igual y el sistema deja de auditar en silencio: la
        // accion ocurre, nadie la registra, y el hueco solo se descubre cuando alguien pregunta
        // que paso y la pantalla contesta que nada.
        services.AddScoped<IActionAuditLog, ActionAuditLog>();
        services.AddScoped<IAccessDecisionLog, AccessDecisionLog>();

        return services;
    }

    public static Assembly ModelConfigurationAssembly => typeof(ServiceCollectionEx).Assembly;
}
