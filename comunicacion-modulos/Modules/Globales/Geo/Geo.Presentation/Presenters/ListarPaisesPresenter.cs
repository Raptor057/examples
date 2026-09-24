using Common.Abstractions;
using Common.ViewModels;
using Geo.Application.UseCases.ListarPaises;

namespace Geo.Presentation.Presenters;

public sealed class ListarPaisesPresenter : IPresenter<ListarPaisesResponse>
{
    private readonly ResultViewModel<GeoController> _vm;

    public ListarPaisesPresenter(ResultViewModel<GeoController> vm) => _vm = vm;

    public Task Handle(ListarPaisesResponse notification, CancellationToken ct)
    {
        if (notification is ListarPaisesSuccess success)
            _vm.OK(success.Paises);

        return Task.CompletedTask;
    }
}
