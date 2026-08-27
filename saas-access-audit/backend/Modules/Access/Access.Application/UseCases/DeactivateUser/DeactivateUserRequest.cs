using Access.Application.UseCases.DeactivateUser.Responses;
using Common.Messaging;

namespace Access.Application.UseCases.DeactivateUser;

/// <summary>
/// La accion sensible del ejemplo.
///
/// Trae DOS cosas y ninguna mas: a quien (por su identificador publico) y por que. No trae quien
/// la ejecuta -eso sale del token- ni de que tenant es -eso tambien-. El MOTIVO es un campo del
/// request porque lo escribe la persona en el dialogo de confirmacion, y sin el la bitacora
/// diria que paso pero no por que, que es justo lo que se busca meses despues.
/// </summary>
public sealed record DeactivateUserRequest(Guid UserPublicId, string? Reason)
    : IRequest<DeactivateUserResponse>;
