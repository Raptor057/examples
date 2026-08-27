using Access.Application.Dtos;
using Access.Application.UseCases.GetUsers.Responses;
using Access.Presentation.Controllers;
using Common.Messaging;
using Common.Results;
using Common.ViewModels;

namespace Access.Presentation.Presenters;

public sealed class GetUsersPresenter(ResultViewModel<AccessController> viewModel)
    : INotificationHandler<GetUsersResponse>
{
    public Task Handle(GetUsersResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure)
            viewModel.Fail(failure.Message);
        else if (response is ISuccess<UsersPageDto> success)
            viewModel.Set(success, data => data);

        return Task.CompletedTask;
    }
}
