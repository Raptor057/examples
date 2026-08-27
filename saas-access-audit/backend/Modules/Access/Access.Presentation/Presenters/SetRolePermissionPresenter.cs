using Access.Application.Dtos;
using Access.Application.UseCases.SetRolePermission.Responses;
using Access.Presentation.Controllers;
using Common.Messaging;
using Common.Results;
using Common.ViewModels;

namespace Access.Presentation.Presenters;

public sealed class SetRolePermissionPresenter(ResultViewModel<AccessController> viewModel)
    : INotificationHandler<SetRolePermissionResponse>
{
    public Task Handle(SetRolePermissionResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure)
            viewModel.Fail(failure.Message);
        else if (response is ISuccess<SetRolePermissionResultDto> success)
            viewModel.Set(success, data => data);

        return Task.CompletedTask;
    }
}
