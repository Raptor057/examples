using Automation.Application.Dtos;
using Common.Messaging;
using Common.Results;

namespace Automation.Application.UseCases.Tasks.RunTaskNow.Responses;

public abstract record RunTaskNowResponse : IResponse;

public sealed record RunTaskNowSuccess(TaskRunNowDto Data)
    : RunTaskNowResponse, ISuccess<TaskRunNowDto>;

public sealed record RunTaskNowValidationFailure(string Message)
    : RunTaskNowResponse, IValidationFailure;

public sealed record RunTaskNowNotFoundFailure(string Message)
    : RunTaskNowResponse, INotFoundFailure;

/// <summary>
/// La tarea esta pausada, o ya la esta corriendo otra instancia. Las dos son conflictos de
/// ESTADO, no errores: 409 y un mensaje que dice cual de las dos es.
///
/// Que "pausada" caiga aqui es el punto entero: si el boton se saltara la pausa, pausar dejaria
/// de ser una garantia y seria una sugerencia.
/// </summary>
public sealed record RunTaskNowConflictFailure(string Message)
    : RunTaskNowResponse, IConflictFailure;
