using Access.Application.Dtos;
using Access.Application.UseCases.GetRoles.Responses;
using Access.Presentation.Controllers;
using Common.Messaging;
using Common.Results;
using Common.ViewModels;

namespace Access.Presentation.Presenters;

public sealed class GetRolesPresenter(ResultViewModel<AccessController> viewModel)
    : INotificationHandler<GetRolesResponse>
{
    public Task Handle(GetRolesResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure)
            viewModel.Fail(failure.Message);
        else if (response is ISuccess<RolesPageDto> success)
            viewModel.Set(success, data => data);

        return Task.CompletedTask;
    }
}
