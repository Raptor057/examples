using Access.Application.Dtos;
using Access.Application.UseCases.GetMyAccess.Responses;
using Access.Presentation.Controllers;
using Common.Messaging;
using Common.Results;
using Common.ViewModels;

namespace Access.Presentation.Presenters;

/// <summary>
/// UN PRESENTER POR CASO DE USO. Sin el, el endpoint responde el envelope VACIO aunque el
/// proyecto compile, el handler funcione y el SQL sea correcto: publicar la respuesta es lo que
/// llena el view model, y si nadie escucha esa publicacion no se llena nada.
/// </summary>
public sealed class GetMyAccessPresenter(ResultViewModel<AccessController> viewModel)
    : INotificationHandler<GetMyAccessResponse>
{
    public Task Handle(GetMyAccessResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure)
            viewModel.Fail(failure.Message);
        else if (response is ISuccess<MyAccessDto> success)
            viewModel.Set(success, data => data);

        return Task.CompletedTask;
    }
}
