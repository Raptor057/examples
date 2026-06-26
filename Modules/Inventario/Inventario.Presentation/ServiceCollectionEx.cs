using Common.Messaging;
using Common.ViewModels;
using Inventario.Application.UseCases.ListarProductos;
using Inventario.Presentation.Presenters;
using Microsoft.Extensions.DependencyInjection;

namespace Inventario.Presentation;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddInventarioPresentationServices(this IServiceCollection services)
    {
        // ResultViewModel<TController> compartido entre controller y presenter (mismo scope).
        services.AddScoped(typeof(ResultViewModel<>));

        // Los presenters se registran como INotificationHandler para que Mediator.Publish los encuentre.
        services.AddScoped<INotificationHandler<ListarProductosResponse>, ListarProductosPresenter>();

        return services;
    }
}
