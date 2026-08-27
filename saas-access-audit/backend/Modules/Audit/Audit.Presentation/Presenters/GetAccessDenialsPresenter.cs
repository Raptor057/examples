using Audit.Application.Dtos;
using Audit.Application.UseCases.GetAccessDenials.Responses;
using Audit.Presentation.Controllers;
using Common.Messaging;
using Common.Results;
using Common.ViewModels;

namespace Audit.Presentation.Presenters;

public sealed class GetAccessDenialsPresenter(ResultViewModel<AuditController> viewModel)
    : INotificationHandler<GetAccessDenialsResponse>
{
    public Task Handle(GetAccessDenialsResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure)
            viewModel.Fail(failure.Message);
        else if (response is ISuccess<AccessDenialPageDto> success)
            viewModel.Set(success, data => data);

        return Task.CompletedTask;
    }
}
