using Common.Results;

namespace Access.Application.UseCases.GetMyAccess.Responses;

public sealed record GetMyAccessNotFoundFailure(string Message)
    : GetMyAccessResponse, INotFoundFailure;
