using Access.Application.UseCases.GetRoles.Responses;
using Common.Messaging;

namespace Access.Application.UseCases.GetRoles;

public sealed record GetRolesRequest : IRequest<GetRolesResponse>;
