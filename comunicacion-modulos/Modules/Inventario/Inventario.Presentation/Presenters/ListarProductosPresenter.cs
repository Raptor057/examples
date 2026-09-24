using Common.Abstractions;
using Common.ViewModels;
using Inventario.Application.UseCases.ListarProductos;

namespace Inventario.Presentation.Presenters;

// Igual que LoginPresenter: el Mediator publica la Response y el Presenter la
// vuelca al ResultViewModel compartido (mismo scope que el controller).
public sealed class ListarProductosPresenter : IPresenter<ListarProductosResponse>
{
    private readonly ResultViewModel<InventarioController> _vm;

    public ListarProductosPresenter(ResultViewModel<InventarioController> vm) => _vm = vm;

    public Task Handle(ListarProductosResponse notification, CancellationToken ct)
    {
        if (notification is ListarProductosSuccess success)
            _vm.OK(success.Productos);

        return Task.CompletedTask;
    }
}
