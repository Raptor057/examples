using Audit.Application.Dtos;
using Common.Results;

namespace Audit.Application.UseCases.GetAccessDenials.Responses;

public sealed record GetAccessDenialsSuccess(AccessDenialPageDto Data)
    : GetAccessDenialsResponse, ISuccess<AccessDenialPageDto>;
