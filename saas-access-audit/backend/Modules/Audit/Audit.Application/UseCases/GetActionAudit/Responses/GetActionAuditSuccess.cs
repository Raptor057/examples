using Audit.Application.Dtos;
using Common.Results;

namespace Audit.Application.UseCases.GetActionAudit.Responses;

public sealed record GetActionAuditSuccess(ActionAuditPageDto Data)
    : GetActionAuditResponse, ISuccess<ActionAuditPageDto>;
