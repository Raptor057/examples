using Access.Application.Dtos;
using Common.Results;

namespace Access.Application.UseCases.SetRolePermission.Responses;

public sealed record SetRolePermissionSuccess(SetRolePermissionResultDto Data)
    : SetRolePermissionResponse, ISuccess<SetRolePermissionResultDto>;
