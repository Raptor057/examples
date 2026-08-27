using Audit.Application.UseCases.GetAccessDenials.Responses;
using Audit.Application.UseCases.GetActionAudit.Responses;
using Audit.Presentation.Presenters;
using Common.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Presentation;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddAuditPresentationServices(this IServiceCollection services)
    {
        // El ResultViewModel generico ya lo registra la presentacion de Access; registrarlo dos
        // veces con AddScoped(typeof(...)) crearia dos entradas para el mismo servicio abierto.
        services.AddScoped<INotificationHandler<GetActionAuditResponse>, GetActionAuditPresenter>();
        services.AddScoped<INotificationHandler<GetAccessDenialsResponse>, GetAccessDenialsPresenter>();

        return services;
    }
}
