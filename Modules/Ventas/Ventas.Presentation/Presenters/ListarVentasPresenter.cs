using Common.Abstractions;
using Common.ViewModels;
using Ventas.Application.UseCases.ListarVentas;

namespace Ventas.Presentation.Presenters;

public sealed class ListarVentasPresenter : IPresenter<ListarVentasResponse>
{
    private readonly ResultViewModel<VentasController> _vm;

    public ListarVentasPresenter(ResultViewModel<VentasController> vm) => _vm = vm;

    public Task Handle(ListarVentasResponse notification, CancellationToken ct)
    {
        if (notification is ListarVentasSuccess success)
            _vm.OK(success.Ventas);

        return Task.CompletedTask;
    }
}
