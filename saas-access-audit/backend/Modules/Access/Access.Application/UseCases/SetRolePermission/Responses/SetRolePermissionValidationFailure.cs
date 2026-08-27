using Common.Results;

namespace Access.Application.UseCases.SetRolePermission.Responses;

public sealed record SetRolePermissionValidationFailure(string Message)
    : SetRolePermissionResponse, IValidationFailure;
