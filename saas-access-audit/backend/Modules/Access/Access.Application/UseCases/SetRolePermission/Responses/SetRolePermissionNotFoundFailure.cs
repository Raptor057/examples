using Common.Results;

namespace Access.Application.UseCases.SetRolePermission.Responses;

public sealed record SetRolePermissionNotFoundFailure(string Message)
    : SetRolePermissionResponse, INotFoundFailure;
