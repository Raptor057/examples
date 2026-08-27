using Automation.Application.Dispatching;
using Automation.Application.Dtos;
using Automation.Application.UseCases.Tasks.ListAutomatedTasks.Responses;
using Automation.Domain.Repositories;
using Automation.Domain.Scheduling;
using Automation.Domain.Tasks;
using Common.Messaging;
using OpsJobs.Kernel;

namespace Automation.Application.UseCases.Tasks.ListAutomatedTasks;

/// <summary>
/// La pantalla de tareas. Sin ella el patron entero es una caja negra que falla en silencio
/// hasta que alguien nota el descuadre semanas despues.
///
/// La lista se arma recorriendo el CATALOGO, no la tabla: asi el orden es el que declara el
/// codigo y una fila que sobre en base no puede aparecer.
/// </summary>
internal sealed class ListAutomatedTasksHandler(
    IAutomatedTaskRepository tasks,
    IAutomatedTaskRunRepository runs,
    SchedulingTimeZone timeZone,
    DispatcherOptions options,
    IOperatorContext operatorContext,
    TimeProvider clock)
    : IRequestHandler<ListAutomatedTasksRequest, ListAutomatedTasksResponse>
{
    public async Task<ListAutomatedTasksResponse> Handle(
        ListAutomatedTasksRequest request, CancellationToken cancellationToken)
    {
        var states = await tasks.ListAsync(AutomatedTaskCatalog.Codes, cancellationToken).ConfigureAwait(false);
        var latestRuns = await runs.GetLatestPerTaskAsync(AutomatedTaskCatalog.Codes, cancellationToken).ConfigureAwait(false);

        var statesByCode = states.ToDictionary(state => state.Code, StringComparer.Ordinal);
        var runsByCode = latestRuns.ToDictionary(run => run.TaskCode, StringComparer.Ordinal);
        var staleClaimBeforeUtc = clock.GetUtcNow().UtcDateTime - options.StaleClaimAfter;

        var items = new List<AutomatedTaskDto>(AutomatedTaskCatalog.All.Count);
        foreach (var definition in AutomatedTaskCatalog.All)
        {
            // Una tarea del catalogo sin fila de configuracion no se inventa aqui: la crea el
            // seeder al arrancar. Si falta, es que el seeder no corrio, y esconderla lo taparia.
            if (!statesByCode.TryGetValue(definition.Code, out var state)) continue;

            runsByCode.TryGetValue(definition.Code, out var lastRun);
            items.Add(AutomationMapper.ToDto(state, lastRun, staleClaimBeforeUtc));
        }

        // El permiso viaja con los datos para que la pantalla pueda esconder lo que no se puede
        // usar. No es el control de acceso: ese esta en la politica del endpoint. Un boton
        // escondido en el cliente no protege nada por si solo.
        return new ListAutomatedTasksSuccess(new AutomatedTaskListDto(
            items, timeZone.Id, operatorContext.Current.CanAdminister));
    }
}
