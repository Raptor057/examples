using Common.Messaging;
using Common.ViewModels;
using Geo.Application.UseCases.ListarPaises;
using Geo.Presentation.Presenters;
using Microsoft.Extensions.DependencyInjection;

namespace Geo.Presentation;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddGeoPresentationServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(ResultViewModel<>));
        services.AddScoped<INotificationHandler<ListarPaisesResponse>, ListarPaisesPresenter>();
        return services;
    }
}
