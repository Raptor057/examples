using Automation.Application.Dispatching;
using Automation.Application.UseCases.Tasks.GetTaskRuns;
using Automation.Application.UseCases.Tasks.GetTaskRuns.Responses;
using Automation.Application.UseCases.Tasks.ListAutomatedTasks;
using Automation.Application.UseCases.Tasks.ListAutomatedTasks.Responses;
using Automation.Application.UseCases.Tasks.RunTaskNow;
using Automation.Application.UseCases.Tasks.RunTaskNow.Responses;
using Automation.Application.UseCases.Tasks.SetTaskEnabled;
using Automation.Application.UseCases.Tasks.SetTaskEnabled.Responses;
using Common.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Automation.Application;

public static class ServiceCollectionEx
{
    /// <summary>
    /// Un registro por handler, escrito a mano. Si falta, el mediador no lo encuentra y la
    /// peticion revienta con el nombre exacto de la interfaz que nadie registro (regla de oro 5).
    /// </summary>
    public static IServiceCollection AddAutomationApplicationServices(this IServiceCollection services)
    {
        // EL despachador. Uno solo, compartido por el temporizador del Host y por el boton de
        // "ejecutar ahora": dos caminos distintos hacia el mismo codigo.
        services.AddScoped<AutomatedTaskDispatcher>();

        services.AddScoped<IRequestHandler<ListAutomatedTasksRequest, ListAutomatedTasksResponse>, ListAutomatedTasksHandler>();
        services.AddScoped<IRequestHandler<GetTaskRunsRequest, GetTaskRunsResponse>, GetTaskRunsHandler>();
        services.AddScoped<IRequestHandler<SetTaskEnabledRequest, SetTaskEnabledResponse>, SetTaskEnabledHandler>();
        services.AddScoped<IRequestHandler<RunTaskNowRequest, RunTaskNowResponse>, RunTaskNowHandler>();

        return services;
    }
}
