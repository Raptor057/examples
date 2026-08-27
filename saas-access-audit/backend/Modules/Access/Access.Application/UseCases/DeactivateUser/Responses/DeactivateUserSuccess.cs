using Access.Application.Dtos;
using Common.Results;

namespace Access.Application.UseCases.DeactivateUser.Responses;

/// <summary>
/// Exito de la parte que si ocurrio. Si el efecto externo quedo pendiente, el DTO lo dice y el
/// presenter pone el aviso en el mensaje del envelope: la accion fue un exito parcial y la
/// pantalla tiene que poder pintarlo como advertencia.
/// </summary>
public sealed record DeactivateUserSuccess(DeactivateUserResultDto Data)
    : DeactivateUserResponse, ISuccess<DeactivateUserResultDto>;
