using Common.Results;

namespace Audit.Application.UseCases.GetAccessDenials.Responses;

public sealed record GetAccessDenialsValidationFailure(string Message)
    : GetAccessDenialsResponse, IValidationFailure;
