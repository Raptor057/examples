using Common.Results;

namespace Audit.Application.UseCases.GetActionAudit.Responses;

public sealed record GetActionAuditValidationFailure(string Message)
    : GetActionAuditResponse, IValidationFailure;
