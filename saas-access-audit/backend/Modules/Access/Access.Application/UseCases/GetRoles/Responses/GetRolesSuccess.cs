using Access.Application.Dtos;
using Common.Results;

namespace Access.Application.UseCases.GetRoles.Responses;

public sealed record GetRolesSuccess(RolesPageDto Data)
    : GetRolesResponse, ISuccess<RolesPageDto>;
