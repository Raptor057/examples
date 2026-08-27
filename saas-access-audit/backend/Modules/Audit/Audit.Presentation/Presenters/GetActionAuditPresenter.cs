using Audit.Application.Dtos;
using Audit.Application.UseCases.GetActionAudit.Responses;
using Audit.Presentation.Controllers;
using Common.Messaging;
using Common.Results;
using Common.ViewModels;

namespace Audit.Presentation.Presenters;

public sealed class GetActionAuditPresenter(ResultViewModel<AuditController> viewModel)
    : INotificationHandler<GetActionAuditResponse>
{
    public Task Handle(GetActionAuditResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure)
            viewModel.Fail(failure.Message);
        else if (response is ISuccess<ActionAuditPageDto> success)
            viewModel.Set(success, data => data);

        return Task.CompletedTask;
    }
}
