namespace Ventas.Domain.Entities;

// Venta guarda el ProductoId (de Inventario), el Vendedor (de Identity) y el
// PaisCliente (validado contra Geo). Ventas no conoce las entidades de esos
// modulos, solo datos planos que recibio via sus contratos.
public sealed class Venta
{
    public Guid Id { get; init; }
    public long ProductoId { get; init; }
    public int Cantidad { get; init; }
    public decimal Total { get; init; }
    public string Vendedor { get; init; }
    public string PaisCliente { get; init; }
    public DateTime FechaUtc { get; init; }

    public Venta(Guid id, long productoId, int cantidad, decimal total, string vendedor, string paisCliente, DateTime fechaUtc)
    {
        Id = id;
        ProductoId = productoId;
        Cantidad = cantidad;
        Total = total;
        Vendedor = vendedor;
        PaisCliente = paisCliente;
        FechaUtc = fechaUtc;
    }
}
