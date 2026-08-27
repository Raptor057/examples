using Common.Messaging;
using Common.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.UseCases.OrdersTree.CreateOrder.Responses;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeChildren.Responses;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeExport.Responses;
using Orders.Presentation.Presenters;

namespace Orders.Presentation;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddOrdersPresentationServices(this IServiceCollection services)
    {
        // El view model generico, uno por peticion y por controller.
        services.AddScoped(typeof(ResultViewModel<>));

        // Un presenter por caso de uso. El registro que falte no rompe el build: rompe el
        // endpoint, en silencio, devolviendo el envelope vacio.
        services.AddScoped<INotificationHandler<GetOrdersTreeChildrenResponse>, GetOrdersTreeChildrenPresenter>();
        services.AddScoped<INotificationHandler<GetOrdersTreeExportResponse>, GetOrdersTreeExportPresenter>();
        services.AddScoped<INotificationHandler<CreateOrderResponse>, CreateOrderPresenter>();

        return services;
    }
}
