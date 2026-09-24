using Common.Abstractions;
using Geo.Contracts;            // <-- global Geo (catalogo)
using Identity.Contracts;       // <-- global Identity (usuario actual)
using Inventario.Contracts;     // <-- modulo de negocio Inventario
using Ventas.Domain.Entities;
using Ventas.Domain.Repositories;

namespace Ventas.Application.UseCases.RegistrarVenta;

// Use case que consume TRES contratos cross-module: un modulo de negocio
// (Inventario) y dos globales (Geo, Identity). Es el patron tipico de un use
// case de negocio en un monolito modular. Todo entra por Contracts; nada por la
// implementacion de esos modulos.
public sealed class RegistrarVentaHandler : IInteractor<RegistrarVentaRequest, RegistrarVentaResponse>
{
    private readonly IConsultaInventario _inventario;     // negocio
    private readonly IGeoCatalog _geo;                    // global
    private readonly ICurrentUserAccessor _currentUser;   // global
    private readonly IVentaRepository _ventas;            // propio

    public RegistrarVentaHandler(
        IConsultaInventario inventario,
        IGeoCatalog geo,
        ICurrentUserAccessor currentUser,
        IVentaRepository ventas)
    {
        _inventario = inventario;
        _geo = geo;
        _currentUser = currentUser;
        _ventas = ventas;
    }

    public async Task<RegistrarVentaResponse> Handle(RegistrarVentaRequest request, CancellationToken cancellationToken)
    {
        // 1. Validar el pais del cliente contra el global Geo.
        if (!await _geo.ExistePaisAsync(request.PaisCliente, cancellationToken))
            return new PaisInvalidoFailure(request.PaisCliente);

        // 2. Pedir el producto al modulo de negocio Inventario.
        var producto = await _inventario.ObtenerProductoAsync(request.ProductoId, cancellationToken);
        if (producto is null)
            return new ProductoNoExisteFailure(request.ProductoId);

        // 3. Descontar stock (Inventario).
        var ajuste = await _inventario.DescontarStockAsync(request.ProductoId, request.Cantidad, cancellationToken);
        if (!ajuste.Exitoso)
            return new StockInsuficienteFailure(ajuste.Error ?? "No se pudo descontar el stock.");

        // 4. Saber quien registra la venta, via el global Identity.
        var vendedor = _currentUser.Nombre;

        // 5. Registrar la venta con los datos reunidos de los tres modulos.
        var total = producto.Precio * request.Cantidad;
        var venta = new Venta(
            Guid.NewGuid(),
            request.ProductoId,
            request.Cantidad,
            total,
            vendedor,
            request.PaisCliente.ToUpperInvariant(),
            DateTime.UtcNow);

        await _ventas.AddAsync(venta, cancellationToken);

        return new RegistrarVentaSuccess(venta.Id, total, ajuste.StockRestante, vendedor, venta.PaisCliente);
    }
}
