using Access.Application.UseCases.GetUsers.Responses;
using Common.Messaging;

namespace Access.Application.UseCases.GetUsers;

/// <summary>
/// Criterios de PRESENTACION -busqueda, pagina, tamano-, que si llegan del cliente y por eso se
/// validan. Los criterios de IDENTIDAD -quien eres, de que tenant- no aparecen aqui: esos salen
/// del token.
/// </summary>
public sealed record GetUsersRequest(
    string? Search,
    bool OnlyActive,
    int? PageNumber,
    int? PageSize) : IRequest<GetUsersResponse>;
