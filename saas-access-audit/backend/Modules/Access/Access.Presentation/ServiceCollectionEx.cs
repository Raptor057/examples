using Access.Application.UseCases.DeactivateUser.Responses;
using Access.Application.UseCases.GetMyAccess.Responses;
using Access.Application.UseCases.GetRoles.Responses;
using Access.Application.UseCases.GetUsers.Responses;
using Access.Application.UseCases.SetRolePermission.Responses;
using Access.Presentation.Presenters;
using Common.Messaging;
using Common.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Access.Presentation;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddAccessPresentationServices(this IServiceCollection services)
    {
        // El view model generico, uno por peticion y por controller.
        services.AddScoped(typeof(ResultViewModel<>));

        // Un presenter por caso de uso. El registro que falte no rompe el build: rompe el
        // endpoint, en silencio, devolviendo el envelope vacio.
        services.AddScoped<INotificationHandler<GetMyAccessResponse>, GetMyAccessPresenter>();
        services.AddScoped<INotificationHandler<GetUsersResponse>, GetUsersPresenter>();
        services.AddScoped<INotificationHandler<DeactivateUserResponse>, DeactivateUserPresenter>();
        services.AddScoped<INotificationHandler<GetRolesResponse>, GetRolesPresenter>();
        services.AddScoped<INotificationHandler<SetRolePermissionResponse>, SetRolePermissionPresenter>();

        return services;
    }
}
