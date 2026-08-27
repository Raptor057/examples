using Common.Messaging;
using Common.Results;
using Common.ViewModels;
using Orders.Application.Dtos;
using Orders.Application.UseCases.OrdersTree.CreateOrder.Responses;
using Orders.Presentation.Controllers;

namespace Orders.Presentation.Presenters;

public sealed class CreateOrderPresenter(ResultViewModel<OrdersTreeController> viewModel)
    : INotificationHandler<CreateOrderResponse>
{
    public Task Handle(CreateOrderResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure)
            viewModel.Fail(failure.Message);
        else if (response is ISuccess<CreatedOrderDto> success)
            viewModel.Set(success, data => data);

        return Task.CompletedTask;
    }
}
