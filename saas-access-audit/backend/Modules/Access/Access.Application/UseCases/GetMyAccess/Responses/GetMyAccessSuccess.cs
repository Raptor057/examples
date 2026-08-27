using Access.Application.Dtos;
using Common.Results;

namespace Access.Application.UseCases.GetMyAccess.Responses;

public sealed record GetMyAccessSuccess(MyAccessDto Data)
    : GetMyAccessResponse, ISuccess<MyAccessDto>;
