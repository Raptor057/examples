using Common.Messaging;
using Common.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Ventas.Application.UseCases.ListarVentas;
using Ventas.Application.UseCases.RegistrarVenta;
using Ventas.Presentation.Presenters;

namespace Ventas.Presentation;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddVentasPresentationServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(ResultViewModel<>));
        services.AddScoped<VentasResponseState>();

        services.AddScoped<INotificationHandler<RegistrarVentaResponse>, RegistrarVentaPresenter>();
        services.AddScoped<INotificationHandler<ListarVentasResponse>, ListarVentasPresenter>();

        return services;
    }
}
