using Access.Application.Dtos;
using Access.Application.UseCases.DeactivateUser.Responses;
using Access.Presentation.Controllers;
using Common.Messaging;
using Common.Results;
using Common.ViewModels;

namespace Access.Presentation.Presenters;

/// <summary>
/// El unico presenter del ejemplo que hace algo mas que proyectar.
///
/// Cuando el efecto externo quedo pendiente, el envelope viaja con isSuccess = true -porque la
/// desactivacion SI ocurrio- pero con un mensaje que lo dice. Es lo que permite a la pantalla
/// pintarlo como ADVERTENCIA en vez de como exito: reportar verde una accion cuyo segundo paso
/// no se completo es mentir, y quien lo lea se ira creyendo que el usuario ya no puede entrar.
/// </summary>
public sealed class DeactivateUserPresenter(ResultViewModel<AccessController> viewModel)
    : INotificationHandler<DeactivateUserResponse>
{
    public Task Handle(DeactivateUserResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure)
        {
            viewModel.Fail(failure.Message);
            return Task.CompletedTask;
        }

        if (response is ISuccess<DeactivateUserResultDto> success)
        {
            viewModel.Set(success, data => data);

            if (!success.Data.ExternalEffectApplied)
            {
                viewModel.SetMessage(
                    "El usuario quedo desactivado, pero la revocacion de sesiones en el proveedor de identidad "
                    + "quedo PENDIENTE. Queda registrada en la bitacora como efecto externo sin aplicar.");
            }
        }

        return Task.CompletedTask;
    }
}
