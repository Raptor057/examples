using Automation.Application.Dtos;
using Common.Messaging;
using Common.Results;

namespace Automation.Application.UseCases.Tasks.SetTaskEnabled.Responses;

public abstract record SetTaskEnabledResponse : IResponse;

public sealed record SetTaskEnabledSuccess(TaskEnabledDto Data)
    : SetTaskEnabledResponse, ISuccess<TaskEnabledDto>;

public sealed record SetTaskEnabledValidationFailure(string Message)
    : SetTaskEnabledResponse, IValidationFailure;

public sealed record SetTaskEnabledNotFoundFailure(string Message)
    : SetTaskEnabledResponse, INotFoundFailure;
