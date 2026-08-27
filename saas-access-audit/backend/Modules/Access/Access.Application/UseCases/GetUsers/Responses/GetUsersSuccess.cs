using Access.Application.Dtos;
using Common.Results;

namespace Access.Application.UseCases.GetUsers.Responses;

public sealed record GetUsersSuccess(UsersPageDto Data)
    : GetUsersResponse, ISuccess<UsersPageDto>;
