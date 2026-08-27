using Common.Results;

namespace Access.Application.UseCases.DeactivateUser.Responses;

public sealed record DeactivateUserValidationFailure(string Message)
    : DeactivateUserResponse, IValidationFailure;
