namespace Inventario.Domain.Entities;

// Entidad de dominio del modulo Inventario. Vive SOLO dentro del modulo;
// nunca se expone fuera (para eso esta el POCO ProductoInfo en Contracts).
public sealed class Producto
{
    public long Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public int Stock { get; private set; }

    public Producto(long id, string nombre, decimal precio, int stock)
    {
        Id = id;
        Nombre = nombre;
        Precio = precio;
        Stock = stock;
    }

    public bool HayStockPara(int cantidad) => cantidad > 0 && Stock >= cantidad;

    public void Descontar(int cantidad)
    {
        if (!HayStockPara(cantidad))
            throw new InvalidOperationException("Stock insuficiente.");
        Stock -= cantidad;
    }
}
