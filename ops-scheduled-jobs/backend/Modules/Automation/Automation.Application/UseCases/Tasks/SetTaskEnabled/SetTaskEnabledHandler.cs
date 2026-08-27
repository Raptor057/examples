using Automation.Application.Dtos;
using Automation.Application.UseCases.Tasks.SetTaskEnabled.Responses;
using Automation.Domain.Repositories;
using Automation.Domain.Scheduling;
using Automation.Domain.Tasks;
using Common.Messaging;

namespace Automation.Application.UseCases.Tasks.SetTaskEnabled;

/// <summary>
/// Pausa y reanuda. Dos detalles que parecen menores y no lo son:
///
/// - **Al pausar se exige motivo.** Es la unica pregunta que alguien va a hacer despues.
/// - **Al reanudar se recalcula la proxima corrida.** La que tenia guardada quedo en el pasado
///   mientras estuvo pausada; dejarla ahi haria que la tarea despertara de golpe con una
///   ventana enorme en el instante de reanudarla.
/// </summary>
internal sealed class SetTaskEnabledHandler(
    IAutomatedTaskRepository tasks,
    SchedulingTimeZone timeZone,
    TimeProvider clock)
    : IRequestHandler<SetTaskEnabledRequest, SetTaskEnabledResponse>
{
    private const int MaxReasonLength = 400;

    public async Task<SetTaskEnabledResponse> Handle(SetTaskEnabledRequest request, CancellationToken cancellationToken)
    {
        var code = (request.Code ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(code))
            return new SetTaskEnabledValidationFailure("Falta el codigo de la tarea.");

        if (!AutomatedTaskCatalog.Contains(code))
            return new SetTaskEnabledNotFoundFailure($"No existe la tarea '{code}'.");

        var reason = (request.Reason ?? string.Empty).Trim();

        if (!request.IsEnabled && string.IsNullOrWhiteSpace(reason))
            return new SetTaskEnabledValidationFailure("Al pausar una tarea hay que decir por que.");

        if (reason.Length > MaxReasonLength)
            return new SetTaskEnabledValidationFailure($"El motivo no puede pasar de {MaxReasonLength} caracteres.");

        var state = await tasks.FindAsync(code, cancellationToken).ConfigureAwait(false);
        if (state is null)
            return new SetTaskEnabledNotFoundFailure($"La tarea '{code}' todavia no tiene configuracion.");

        var nowUtc = clock.GetUtcNow().UtcDateTime;

        // Al reanudar, la proxima corrida se recalcula desde AHORA. Al pausar no hace falta
        // tocarla: la tarea no se va a reclamar de todas formas.
        DateTime? nextRunAtUtc = request.IsEnabled
            ? TaskScheduleCalculator.ComputeNextRunUtc(state.Schedule, nowUtc, timeZone)
            : null;

        var updated = await tasks.SetEnabledAsync(
            code,
            request.IsEnabled,
            request.IsEnabled ? null : reason,
            nextRunAtUtc,
            cancellationToken).ConfigureAwait(false);

        if (!updated)
            return new SetTaskEnabledNotFoundFailure($"La tarea '{code}' todavia no tiene configuracion.");

        return new SetTaskEnabledSuccess(new TaskEnabledDto(
            code,
            request.IsEnabled,
            request.IsEnabled ? null : reason,
            nextRunAtUtc ?? state.NextRunAtUtc));
    }
}
