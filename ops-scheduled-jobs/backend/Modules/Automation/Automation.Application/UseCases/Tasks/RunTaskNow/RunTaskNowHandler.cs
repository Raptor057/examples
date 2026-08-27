using Automation.Application.Dispatching;
using Automation.Application.Dtos;
using Automation.Application.UseCases.Tasks.RunTaskNow.Responses;
using Automation.Domain.Repositories;
using Common.Messaging;
using OpsJobs.Kernel;

namespace Automation.Application.UseCases.Tasks.RunTaskNow;

/// <summary>
/// Disparar una tarea a mano. Reusa EL MISMO despachador que el temporizador: mismo reclamo,
/// misma ventana desde el cursor, misma bitacora, mismo finally.
///
/// Un camino manual con su propia logica es como se llega a que "ejecutar ahora" mueva el
/// cursor distinto que la corrida automatica, o no deje rastro en la bitacora, y a que nadie
/// entienda por que los numeros no cuadran.
/// </summary>
internal sealed class RunTaskNowHandler(
    AutomatedTaskDispatcher dispatcher,
    IAutomatedTaskRepository tasks,
    IOperatorContext operatorContext)
    : IRequestHandler<RunTaskNowRequest, RunTaskNowResponse>
{
    public async Task<RunTaskNowResponse> Handle(RunTaskNowRequest request, CancellationToken cancellationToken)
    {
        var code = (request.Code ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(code))
            return new RunTaskNowValidationFailure("Falta el codigo de la tarea.");

        var operatorName = operatorContext.Current.UserName;
        if (string.IsNullOrWhiteSpace(operatorName))
            return new RunTaskNowValidationFailure("No se pudo identificar a quien dispara la tarea.");

        var report = await dispatcher.RunNowAsync(code, operatorName, cancellationToken).ConfigureAwait(false);

        switch (report.Outcome)
        {
            case DispatchOutcome.UnknownTask:
                return new RunTaskNowNotFoundFailure($"No existe la tarea '{code}'.");

            case DispatchOutcome.NotConfigured:
                return new RunTaskNowNotFoundFailure($"La tarea '{code}' todavia no tiene configuracion.");

            case DispatchOutcome.Paused:
                var state = await tasks.FindAsync(code, cancellationToken).ConfigureAwait(false);
                var reason = string.IsNullOrWhiteSpace(state?.PausedReason) ? null : state!.PausedReason;
                return new RunTaskNowConflictFailure(reason is null
                    ? "La tarea esta pausada. Reanudala antes de ejecutarla."
                    : $"La tarea esta pausada: {reason}");

            case DispatchOutcome.AlreadyClaimed:
                return new RunTaskNowConflictFailure("La tarea ya la esta ejecutando otra instancia.");

            case DispatchOutcome.Executed:
                // El resultado que se devuelve es el REAL: que ventana entro, cuantos elementos
                // se procesaron, cuantos fallaron y hasta donde llego el cursor. Un "listo" sin
                // datos deja al operador igual de ciego que antes de apretar el boton.
                var result = report.Result!;
                var window = report.Window!;

                return new RunTaskNowSuccess(new TaskRunNowDto(
                    code,
                    AutomatedTaskDispatcher.ToStorageStatus(result.Status),
                    result.ItemsProcessed,
                    result.ItemsFailed,
                    result.Message,
                    window.FromUtc,
                    window.ToUtc,
                    result.CursorAfterUtc));

            default:
                return new RunTaskNowValidationFailure("Resultado de despacho desconocido.");
        }
    }
}
