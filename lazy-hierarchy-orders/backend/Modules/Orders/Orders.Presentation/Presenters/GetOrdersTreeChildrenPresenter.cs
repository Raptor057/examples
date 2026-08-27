using Common.Messaging;
using Common.Results;
using Common.ViewModels;
using Orders.Application.Dtos;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeChildren.Responses;
using Orders.Presentation.Controllers;

namespace Orders.Presentation.Presenters;

/// <summary>
/// Un presenter por caso de uso. Sin el, el endpoint responde el envelope vacio aunque el
/// proyecto compile en cero errores y la consulta sea correcta: nadie habria llenado el view
/// model. Es invisible para el compilador y solo se ve llamando la API.
/// </summary>
public sealed class GetOrdersTreeChildrenPresenter(ResultViewModel<OrdersTreeController> viewModel)
    : INotificationHandler<GetOrdersTreeChildrenResponse>
{
    public Task Handle(GetOrdersTreeChildrenResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure)
            viewModel.Fail(failure.Message);
        else if (response is ISuccess<OrdersTreeChildrenDto> success)
            viewModel.Set(success, data => new
            {
                level = data.Level,
                items = data.Items,
                totalCount = data.TotalCount,
                pageNumber = data.PageNumber,
                pageSize = data.PageSize,
                hasMore = data.HasMore
            });

        return Task.CompletedTask;
    }
}
