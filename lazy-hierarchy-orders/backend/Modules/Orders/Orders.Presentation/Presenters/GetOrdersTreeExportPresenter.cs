using Common.Messaging;
using Common.Results;
using Common.ViewModels;
using Orders.Application.Dtos;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeExport.Responses;
using Orders.Presentation.Controllers;

namespace Orders.Presentation.Presenters;

public sealed class GetOrdersTreeExportPresenter(ResultViewModel<OrdersTreeController> viewModel)
    : INotificationHandler<GetOrdersTreeExportResponse>
{
    public Task Handle(GetOrdersTreeExportResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure)
            viewModel.Fail(failure.Message);
        else if (response is ISuccess<OrdersExportPageDto> success)
            viewModel.Set(success, data => new
            {
                columns = data.Columns,
                items = data.Items,
                totalCount = data.TotalCount,
                pageNumber = data.PageNumber,
                pageSize = data.PageSize
            });

        return Task.CompletedTask;
    }
}
