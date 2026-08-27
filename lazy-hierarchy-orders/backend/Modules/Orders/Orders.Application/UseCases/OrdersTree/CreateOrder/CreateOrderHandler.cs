using Common.Messaging;
using Orders.Application.Dtos;
using Orders.Application.UseCases.OrdersTree.CreateOrder.Responses;
using Orders.Domain.Entities;
using Orders.Domain.Repositories;

namespace Orders.Application.UseCases.OrdersTree.CreateOrder;

/// <summary>
/// El lado de escritura. Existe para que el ejemplo no se quede solo con lecturas: aqui se ve
/// que el handler nunca menciona el tenant y aun asi la fila queda sellada con el correcto.
/// </summary>
internal sealed class CreateOrderHandler(IOrderWriteRepository repository)
    : IRequestHandler<CreateOrderRequest, CreateOrderResponse>
{
    private const int MaxLines = 100;

    public async Task<CreateOrderResponse> Handle(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var orderNumber = (request.OrderNumber ?? string.Empty).Trim().ToUpperInvariant();
        var customerName = (request.CustomerName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(orderNumber))
            return new CreateOrderValidationFailure("El numero de pedido es obligatorio.");

        if (string.IsNullOrWhiteSpace(customerName))
            return new CreateOrderValidationFailure("El nombre del cliente es obligatorio.");

        var lines = request.Lines ?? [];
        if (lines.Count == 0)
            return new CreateOrderValidationFailure("El pedido necesita al menos una linea.");

        if (lines.Count > MaxLines)
            return new CreateOrderValidationFailure($"Un pedido no puede tener mas de {MaxLines} lineas.");

        if (lines.Any(line => line.Quantity <= 0))
            return new CreateOrderValidationFailure("La cantidad de cada linea tiene que ser mayor que cero.");

        // La unicidad se consulta por EF, asi que el global query filter ya la acota al tenant
        // actual: dos empresas pueden usar el mismo numero de pedido sin chocar.
        if (await repository.OrderNumberExistsAsync(orderNumber, cancellationToken).ConfigureAwait(false))
            return new CreateOrderConflictFailure("Ya existe un pedido con ese numero.");

        var order = new CustomerOrder
        {
            OrderNumber = orderNumber,
            CustomerName = customerName,
            Status = "placed",
            PlacedAtUtc = DateTime.UtcNow
        };

        decimal total = 0m;
        foreach (var line in lines)
        {
            var product = await repository.GetProductAsync(line.ProductPublicId, cancellationToken).ConfigureAwait(false);
            if (product is null)
                return new CreateOrderNotFoundFailure($"No se encontro el producto {line.ProductPublicId}.");

            var lineTotal = decimal.Round(product.UnitPrice * line.Quantity, 2);
            total += lineTotal;

            order.Lines.Add(new OrderLine
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                UnitPrice = product.UnitPrice,
                LineTotal = lineTotal
            });
        }

        order.TotalAmount = total;
        var publicId = await repository.CreateAsync(order, cancellationToken).ConfigureAwait(false);

        return new CreateOrderSuccess(new CreatedOrderDto(publicId, order.OrderNumber, order.TotalAmount, order.Lines.Count));
    }
}
