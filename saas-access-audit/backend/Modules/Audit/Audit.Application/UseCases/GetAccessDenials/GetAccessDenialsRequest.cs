using Audit.Application.UseCases.GetAccessDenials.Responses;
using Common.Messaging;

namespace Audit.Application.UseCases.GetAccessDenials;

public sealed record GetAccessDenialsRequest(
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? PermissionCode,
    string? AttemptedBy,
    bool OnlyWouldHaveBeenBlocked,
    int? PageNumber,
    int? PageSize) : IRequest<GetAccessDenialsResponse>;
