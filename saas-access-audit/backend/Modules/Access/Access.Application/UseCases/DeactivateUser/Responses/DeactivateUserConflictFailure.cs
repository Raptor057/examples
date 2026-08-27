using Common.Results;

namespace Access.Application.UseCases.DeactivateUser.Responses;

/// <summary>
/// El usuario ya estaba desactivado. No es un error del sistema: es el segundo clic sobre una
/// accion que ya se ejecuto, y responderlo como conflicto -en vez de repetir la escritura- es lo
/// que impide que la bitacora se llene de renglones identicos.
/// </summary>
public sealed record DeactivateUserConflictFailure(string Message)
    : DeactivateUserResponse, IConflictFailure;
