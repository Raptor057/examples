namespace Geo.Domain.Entities;

public sealed class Pais
{
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;

    public Pais(string codigo, string nombre)
    {
        Codigo = codigo;
        Nombre = nombre;
    }
}
