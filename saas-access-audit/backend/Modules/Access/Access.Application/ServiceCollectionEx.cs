using Access.Application.UseCases.DeactivateUser;
using Access.Application.UseCases.DeactivateUser.Responses;
using Access.Application.UseCases.GetMyAccess;
using Access.Application.UseCases.GetMyAccess.Responses;
using Access.Application.UseCases.GetRoles;
using Access.Application.UseCases.GetRoles.Responses;
using Access.Application.UseCases.GetUsers;
using Access.Application.UseCases.GetUsers.Responses;
using Access.Application.UseCases.SetRolePermission;
using Access.Application.UseCases.SetRolePermission.Responses;
using Common.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Access.Application;

public static class ServiceCollectionEx
{
    /// <summary>
    /// Un registro por handler, escrito a mano. Si falta, el mediador no lo encuentra y la
    /// peticion revienta con el nombre exacto de la interfaz que nadie registro. Es mas verboso
    /// que un escaneo de ensamblados y es a proposito: un registro que falta es invisible para el
    /// compilador.
    /// </summary>
    public static IServiceCollection AddAccessApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<GetMyAccessRequest, GetMyAccessResponse>, GetMyAccessHandler>();
        services.AddScoped<IRequestHandler<GetUsersRequest, GetUsersResponse>, GetUsersHandler>();
        services.AddScoped<IRequestHandler<DeactivateUserRequest, DeactivateUserResponse>, DeactivateUserHandler>();
        services.AddScoped<IRequestHandler<GetRolesRequest, GetRolesResponse>, GetRolesHandler>();
        services.AddScoped<IRequestHandler<SetRolePermissionRequest, SetRolePermissionResponse>, SetRolePermissionHandler>();
        return services;
    }
}
