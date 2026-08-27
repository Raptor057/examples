using Automation.Application.Dtos;
using Common.Messaging;
using Common.Results;

namespace Automation.Application.UseCases.Tasks.GetTaskRuns.Responses;

public abstract record GetTaskRunsResponse : IResponse;

public sealed record GetTaskRunsSuccess(TaskRunPageDto Data)
    : GetTaskRunsResponse, ISuccess<TaskRunPageDto>;

public sealed record GetTaskRunsValidationFailure(string Message)
    : GetTaskRunsResponse, IValidationFailure;

/// <summary>
/// Un codigo que no esta en el catalogo es 404, no una lista vacia. Devolver vacio haria pasar
/// por "esta tarea nunca corrio" lo que en realidad es "esta tarea no existe".
/// </summary>
public sealed record GetTaskRunsNotFoundFailure(string Message)
    : GetTaskRunsResponse, INotFoundFailure;
