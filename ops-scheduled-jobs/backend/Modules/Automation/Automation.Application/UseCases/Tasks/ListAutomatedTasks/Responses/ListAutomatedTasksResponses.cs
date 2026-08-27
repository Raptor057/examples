using Automation.Application.Dtos;
using Common.Messaging;
using Common.Results;

namespace Automation.Application.UseCases.Tasks.ListAutomatedTasks.Responses;

/// <summary>
/// El tipo BASE de respuesta del caso de uso. El presenter se registra sobre ESTE tipo, no
/// sobre el de exito: el mediador publica por tipo estatico, asi que un presenter registrado
/// sobre la variante concreta nunca se llamaria.
/// </summary>
public abstract record ListAutomatedTasksResponse : IResponse;

public sealed record ListAutomatedTasksSuccess(AutomatedTaskListDto Data)
    : ListAutomatedTasksResponse, ISuccess<AutomatedTaskListDto>;
