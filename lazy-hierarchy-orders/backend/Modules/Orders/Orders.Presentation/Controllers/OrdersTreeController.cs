using Common.Messaging;
using Common.ViewModels;
using LazyHierarchy.Shared.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.UseCases.OrdersTree.CreateOrder;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeChildren;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeExport;

namespace Orders.Presentation.Controllers;

/// <summary>
/// Toda la superficie HTTP del arbol: UN endpoint de niveles, uno de exportacion y uno de alta.
/// [Authorize] no es decorado: sin token no hay claim de tenant, y sin claim el filtro compara
/// contra el contexto sistema y no devuelve nada. El candado y el aislamiento son el mismo hilo.
/// </summary>
[ApiController]
[Authorize]
public sealed class OrdersTreeController(
    IMediator mediator,
    ResultViewModel<OrdersTreeController> viewModel) : BaseApiController(mediator)
{
    /// <summary>
    /// El unico endpoint de niveles. Sirve los seis: el "level" decide cual, y sale de una lista
    /// blanca del servidor. El cliente nunca lo inventa; reenvia el nextLevel que recibio.
    /// </summary>
    [HttpGet("/api/orders-tree/children")]
    public async Task<IActionResult> GetChildren(
        [FromQuery] string? level,
        [FromQuery] string? searchType,
        [FromQuery] string? searchValue,
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] long? categoryId,
        [FromQuery] long? productId,
        [FromQuery] long? orderId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var response = await DispatchAsync(
            new GetOrdersTreeChildrenRequest(
                level, searchType, searchValue, year, month, categoryId, productId, orderId, pageNumber, pageSize),
            cancellationToken);

        return MapResult(response, viewModel);
    }

    /// <summary>Matriz plana del nodo, por paginas. Las coordenadas son las mismas del arbol.</summary>
    [HttpGet("/api/orders-tree/export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? searchType,
        [FromQuery] string? searchValue,
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] long? categoryId,
        [FromQuery] long? productId,
        [FromQuery] long? orderId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var response = await DispatchAsync(
            new GetOrdersTreeExportRequest(
                searchType, searchValue, year, month, categoryId, productId, orderId, pageNumber, pageSize),
            cancellationToken);

        return MapResult(response, viewModel);
    }

    [HttpPost("/api/orders")]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest body,
        CancellationToken cancellationToken = default)
    {
        var response = await DispatchAsync(body, cancellationToken);
        return MapResult(response, viewModel);
    }
}
