using Access.Application.UseCases.GetMyAccess.Responses;
using Common.Messaging;

namespace Access.Application.UseCases.GetMyAccess;

/// <summary>
/// Sin un solo parametro, y eso es el punto: QUIEN pregunta sale del token.
///
/// Si el request llevara un campo de usuario o de tenant, cualquiera podria leer los permisos de
/// otro cambiando un valor en la peticion. Hay una prueba que recorre todos los Request del
/// modulo y falla si alguien agrega un campo con esos nombres.
/// </summary>
public sealed record GetMyAccessRequest : IRequest<GetMyAccessResponse>;
