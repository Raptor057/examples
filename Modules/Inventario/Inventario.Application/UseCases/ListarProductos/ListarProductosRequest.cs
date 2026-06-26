using Common.Messaging;

namespace Inventario.Application.UseCases.ListarProductos;

public sealed record ListarProductosRequest : IRequest<ListarProductosResponse>;
