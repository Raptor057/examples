using System.Reflection;
using Orders.Application.UseCases.OrdersTree.CreateOrder;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeChildren;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeExport;
using Orders.Domain.Trees;

namespace Orders.Tests;

/// <summary>
/// Reglas de estructura que una revision se olvida y una prueba roja no.
/// </summary>
public sealed class ArchitectureAndContractTests
{
    private static readonly Assembly DomainAssembly = typeof(OrdersTreeLevels).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(GetOrdersTreeChildrenHandler).Assembly;
    private static readonly Assembly InfrastructureAssembly =
        typeof(Orders.Infrastructure.ServiceCollectionEx).Assembly;

    [Fact]
    public void Application_no_referencia_Infrastructure()
    {
        var referenced = ApplicationAssembly.GetReferencedAssemblies().Select(name => name.Name).ToList();
        Assert.DoesNotContain("Orders.Infrastructure", referenced);
    }

    [Fact]
    public void Infrastructure_no_referencia_Application()
    {
        var referenced = InfrastructureAssembly.GetReferencedAssemblies().Select(name => name.Name).ToList();
        Assert.DoesNotContain("Orders.Application", referenced);
    }

    [Fact]
    public void Domain_no_conoce_ni_el_mediador_ni_el_orm()
    {
        var referenced = DomainAssembly.GetReferencedAssemblies().Select(name => name.Name).ToList();
        Assert.DoesNotContain("Common", referenced);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", referenced);
        Assert.DoesNotContain("Dapper", referenced);
        Assert.DoesNotContain("LazyHierarchy.Shared", referenced);
    }

    public static TheoryData<Type> RequestTypes() => new()
    {
        typeof(GetOrdersTreeChildrenRequest),
        typeof(GetOrdersTreeExportRequest),
        typeof(CreateOrderRequest)
    };

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void Ningun_request_acepta_el_tenant_desde_el_cliente(Type requestType)
    {
        // Si el tenant fuera un parametro del request, cualquiera leeria los datos de otra
        // empresa cambiando un numero en la URL. Sale del token y de ningun otro lado.
        var offending = requestType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.Name.Contains("Tenant", StringComparison.OrdinalIgnoreCase))
            .Select(property => property.Name)
            .ToList();

        Assert.True(
            offending.Count == 0,
            $"{requestType.Name} expone {string.Join(", ", offending)}. El tenant nunca llega del cliente.");
    }

    [Fact]
    public void La_jerarquia_encadena_los_seis_niveles_y_termina_en_la_hoja()
    {
        // El cliente no conoce la jerarquia: la reconstruye siguiendo nextLevel. Esta prueba
        // vigila que la cadena este completa y que la hoja realmente sea hoja.
        var chain = new List<string>();
        var current = OrdersTreeLevels.Year;
        while (current is not null)
        {
            chain.Add(current);
            current = OrdersTreeLevels.NextOf(current);
        }

        Assert.Equal(
            [
                OrdersTreeLevels.Year,
                OrdersTreeLevels.Month,
                OrdersTreeLevels.Category,
                OrdersTreeLevels.Product,
                OrdersTreeLevels.Order,
                OrdersTreeLevels.Leaf
            ],
            chain);

        Assert.Null(OrdersTreeLevels.NextOf(OrdersTreeLevels.Leaf));
        Assert.Equal(chain.Count, OrdersTreeLevels.Allowed.Count);
    }

    [Fact]
    public void Solo_el_nivel_de_fan_out_pagina()
    {
        Assert.True(OrdersTreeLevels.IsPaged(OrdersTreeLevels.Order));
        Assert.False(OrdersTreeLevels.IsPaged(OrdersTreeLevels.Year));
        Assert.False(OrdersTreeLevels.IsPaged(OrdersTreeLevels.Month));
        Assert.False(OrdersTreeLevels.IsPaged(OrdersTreeLevels.Category));
        Assert.False(OrdersTreeLevels.IsPaged(OrdersTreeLevels.Product));
        Assert.False(OrdersTreeLevels.IsPaged(OrdersTreeLevels.Leaf));
    }
}
