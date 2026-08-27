using Common.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.UseCases.OrdersTree.CreateOrder;
using Orders.Application.UseCases.OrdersTree.CreateOrder.Responses;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeChildren;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeChildren.Responses;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeExport;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeExport.Responses;

namespace Orders.Application;

public static class ServiceCollectionEx
{
    /// <summary>
    /// Un registro por handler, escrito a mano. Si falta, el mediador no lo encuentra y la
    /// peticion revienta con el nombre exacto de la interfaz que nadie registro.
    /// </summary>
    public static IServiceCollection AddOrdersApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<GetOrdersTreeChildrenRequest, GetOrdersTreeChildrenResponse>, GetOrdersTreeChildrenHandler>();
        services.AddScoped<IRequestHandler<GetOrdersTreeExportRequest, GetOrdersTreeExportResponse>, GetOrdersTreeExportHandler>();
        services.AddScoped<IRequestHandler<CreateOrderRequest, CreateOrderResponse>, CreateOrderHandler>();
        return services;
    }
}
