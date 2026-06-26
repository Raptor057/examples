using Common.Results;
using Inventario.Contracts;

namespace Inventario.Application.UseCases.ListarProductos;

public sealed record ListarProductosSuccess(IReadOnlyList<ProductoInfo> Productos)
    : ListarProductosResponse, ISuccess;
