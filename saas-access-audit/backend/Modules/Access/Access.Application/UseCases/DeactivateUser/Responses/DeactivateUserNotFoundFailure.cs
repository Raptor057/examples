using Common.Results;

namespace Access.Application.UseCases.DeactivateUser.Responses;

public sealed record DeactivateUserNotFoundFailure(string Message)
    : DeactivateUserResponse, INotFoundFailure;
