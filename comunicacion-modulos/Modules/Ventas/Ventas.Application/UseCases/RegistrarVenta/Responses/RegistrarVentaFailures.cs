using Common.Results;

namespace Ventas.Application.UseCases.RegistrarVenta;

// Cada fallo es un tipo distinto; el Presenter los traduce a codigos HTTP.
public sealed record ProductoNoExisteFailure(long ProductoId) : RegistrarVentaResponse, INotFoundFailure
{
    public string Message => $"El producto {ProductoId} no existe.";
}

public sealed record StockInsuficienteFailure(string Detalle) : RegistrarVentaResponse, IValidationFailure
{
    public string Message => Detalle;
}

// Fallo originado por la validacion contra el global Geo.
public sealed record PaisInvalidoFailure(string Codigo) : RegistrarVentaResponse, IValidationFailure
{
    public string Message => $"El pais '{Codigo}' no existe en el catalogo (Geo).";
}
