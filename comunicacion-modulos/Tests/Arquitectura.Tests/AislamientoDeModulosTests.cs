using NetArchTest.Rules;
using Ventas.Application.UseCases.RegistrarVenta;

namespace Arquitectura.Tests;

// Convierte la regla central del patron ("los modulos solo se comunican via Contracts")
// en algo verificable por CI con NetArchTest.Rules.
public class AislamientoDeModulosTests
{
    private static readonly System.Reflection.Assembly VentasApplication = typeof(RegistrarVentaHandler).Assembly;

    [Fact]
    public void Ventas_solo_depende_de_Contracts_no_de_implementaciones()
    {
        var resultado = Types.InAssembly(VentasApplication)
            .That().ResideInNamespaceStartingWith("Ventas")
            .ShouldNot().HaveDependencyOnAny(
                // negocio
                "Inventario.Domain", "Inventario.Application", "Inventario.Infrastructure",
                // global Identity
                "Identity.Infrastructure",
                // global Geo
                "Geo.Domain", "Geo.Application", "Geo.Infrastructure")
            .GetResult();

        var infractores = resultado.FailingTypeNames is null
            ? string.Empty
            : string.Join(", ", resultado.FailingTypeNames);

        Assert.True(
            resultado.IsSuccessful,
            $"Ventas debe hablar con los otros modulos SOLO via *.Contracts. Tipos que rompen la regla: {infractores}");
    }

    [Fact]
    public void El_handler_de_ventas_se_acopla_a_los_contratos_de_negocio_y_globales()
    {
        var ctor = typeof(RegistrarVentaHandler).GetConstructors().Single();
        var tipos = ctor.GetParameters().Select(p => p.ParameterType.FullName).ToList();

        // Negocio + los dos globales: todo entra por su contrato.
        Assert.Contains("Inventario.Contracts.IConsultaInventario", tipos);
        Assert.Contains("Geo.Contracts.IGeoCatalog", tipos);
        Assert.Contains("Identity.Contracts.ICurrentUserAccessor", tipos);
    }
}
