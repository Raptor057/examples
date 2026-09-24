using Common.Abstractions;
using Common.Results;
using Common.ViewModels;
using Ventas.Application.UseCases.RegistrarVenta;

namespace Ventas.Presentation.Presenters;

public sealed class RegistrarVentaPresenter : IPresenter<RegistrarVentaResponse>
{
    private readonly ResultViewModel<VentasController> _vm;
    private readonly VentasResponseState _state;

    public RegistrarVentaPresenter(ResultViewModel<VentasController> vm, VentasResponseState state)
    {
        _vm = vm;
        _state = state;
    }

    public Task Handle(RegistrarVentaResponse notification, CancellationToken ct)
    {
        switch (notification)
        {
            case RegistrarVentaSuccess s:
                _vm.OK(new { s.VentaId, s.Total, s.StockRestante, s.Vendedor, s.PaisCliente });
                break;

            case ProductoNoExisteFailure f:
                _state.HttpStatusCode = 404;
                _vm.Fail(f.Message);
                break;

            case IFailure f:   // StockInsuficiente / PaisInvalido
                _state.HttpStatusCode = 400;
                _vm.Fail(f.Message);
                break;
        }

        return Task.CompletedTask;
    }
}
