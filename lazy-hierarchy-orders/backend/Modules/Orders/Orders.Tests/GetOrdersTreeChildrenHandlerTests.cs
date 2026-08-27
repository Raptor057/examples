using Common.Results;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeChildren;
using Orders.Application.UseCases.OrdersTree.GetOrdersTreeChildren.Responses;
using Orders.Domain.Trees;

namespace Orders.Tests;

public sealed class GetOrdersTreeChildrenHandlerTests
{
    private static GetOrdersTreeChildrenRequest Request(
        string? level,
        int? year = null,
        int? month = null,
        long? categoryId = null,
        long? productId = null,
        long? orderId = null,
        string? searchType = "all",
        string? searchValue = null,
        int? pageNumber = null,
        int? pageSize = null)
        => new(level, searchType, searchValue, year, month, categoryId, productId, orderId, pageNumber, pageSize);

    [Theory]
    [InlineData("tree-anything")]
    [InlineData("DROP TABLE order_line")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Un_nivel_fuera_de_la_lista_blanca_no_llega_al_repositorio(string? level)
    {
        var repository = new FakeOrdersTreeReadRepository();
        var handler = new GetOrdersTreeChildrenHandler(repository);

        var response = await handler.Handle(Request(level), CancellationToken.None);

        Assert.IsType<GetOrdersTreeChildrenValidationFailure>(response);
        Assert.False(repository.WasCalled);
    }

    [Fact]
    public async Task Un_tipo_de_busqueda_desconocido_se_rechaza_antes_del_sql()
    {
        var repository = new FakeOrdersTreeReadRepository();
        var handler = new GetOrdersTreeChildrenHandler(repository);

        var response = await handler.Handle(
            Request(OrdersTreeLevels.Year, searchType: "sql-injection", searchValue: "x"), CancellationToken.None);

        var failure = Assert.IsType<GetOrdersTreeChildrenValidationFailure>(response);
        Assert.Equal("El tipo de busqueda no es valido.", failure.Message);
        Assert.False(repository.WasCalled);
    }

    public static TheoryData<string, int?, int?, long?, long?, long?, string> MissingCoordinates() => new()
    {
        { OrdersTreeLevels.Month, null, null, null, null, null, "Falta el anio." },
        { OrdersTreeLevels.Category, 2026, null, null, null, null, "Falta el mes." },
        { OrdersTreeLevels.Product, 2026, 3, null, null, null, "Falta la categoria." },
        { OrdersTreeLevels.Order, 2026, 3, 10L, null, null, "Falta el producto." },
        { OrdersTreeLevels.Leaf, null, null, null, null, null, "Falta el pedido." }
    };

    [Theory]
    [MemberData(nameof(MissingCoordinates))]
    public async Task Cada_nivel_exige_sus_coordenadas_con_mensaje_propio(
        string level, int? year, int? month, long? categoryId, long? productId, long? orderId, string expectedMessage)
    {
        // Un nivel profundo sin sus coordenadas no es una consulta vacia: es una consulta SIN
        // FILTRO sobre millones de filas. Por eso se corta antes de tocar el repositorio.
        var repository = new FakeOrdersTreeReadRepository();
        var handler = new GetOrdersTreeChildrenHandler(repository);

        var response = await handler.Handle(
            Request(level, year, month, categoryId, productId, orderId), CancellationToken.None);

        var failure = Assert.IsType<GetOrdersTreeChildrenValidationFailure>(response);
        Assert.Equal(expectedMessage, failure.Message);
        Assert.False(repository.WasCalled);
    }

    [Fact]
    public async Task El_tamano_de_pagina_del_cliente_se_topa()
    {
        var repository = new FakeOrdersTreeReadRepository();
        var handler = new GetOrdersTreeChildrenHandler(repository);

        await handler.Handle(
            Request(OrdersTreeLevels.Order, 2026, 3, 10L, 55L, pageNumber: 2, pageSize: 100_000),
            CancellationToken.None);

        Assert.Equal(200, repository.LastPageSize);
        Assert.Equal(2, repository.LastPageNumber);
    }

    [Fact]
    public async Task El_nodo_devuelve_su_nextLevel_y_sus_coordenadas()
    {
        var repository = new FakeOrdersTreeReadRepository
        {
            Items =
            [
                new OrdersTreeNode(
                    Id: "y:2026",
                    NodeType: "year",
                    Label: "2026",
                    NextLevel: OrdersTreeLevels.Month,
                    Coordinates: new OrdersTreeCoordinates(Year: 2026),
                    Metrics: new OrdersTreeMetrics(OrderCount: 1204),
                    Details: new Dictionary<string, string?>(),
                    Children: [])
            ]
        };

        var handler = new GetOrdersTreeChildrenHandler(repository);
        var response = await handler.Handle(Request(OrdersTreeLevels.Year), CancellationToken.None);

        var success = Assert.IsType<GetOrdersTreeChildrenSuccess>(response);
        var node = Assert.Single(success.Data.Items);
        Assert.Equal(OrdersTreeLevels.Month, node.NextLevel);
        Assert.Equal(2026, node.Year);
        Assert.Equal(1204, node.Metrics.OrderCount);
    }

    [Fact]
    public async Task La_hoja_devuelve_su_subarbol_completo_con_nextLevel_nulo()
    {
        var repository = new FakeOrdersTreeReadRepository
        {
            Items =
            [
                new OrdersTreeNode(
                    Id: "o:7:lines",
                    NodeType: "orderLineGroup",
                    Label: "ACME-00000007",
                    NextLevel: null,
                    Coordinates: new OrdersTreeCoordinates(OrderId: 7),
                    Metrics: new OrdersTreeMetrics(LineCount: 1),
                    Details: new Dictionary<string, string?>(),
                    Children:
                    [
                        new OrdersTreeNode(
                            Id: "ol:11",
                            NodeType: "orderLine",
                            Label: "Teclado",
                            NextLevel: null,
                            Coordinates: new OrdersTreeCoordinates(OrderId: 7),
                            Metrics: new OrdersTreeMetrics(UnitCount: 3),
                            Details: new Dictionary<string, string?>(),
                            Children: [])
                    ])
            ]
        };

        var handler = new GetOrdersTreeChildrenHandler(repository);
        var response = await handler.Handle(Request(OrdersTreeLevels.Leaf, orderId: 7), CancellationToken.None);

        var success = Assert.IsType<GetOrdersTreeChildrenSuccess>(response);
        var group = Assert.Single(success.Data.Items);
        Assert.Null(group.NextLevel);
        var line = Assert.Single(group.Children);
        Assert.Null(line.NextLevel);
        Assert.Equal("ol:11", line.Id);
    }

    [Fact]
    public async Task El_fallo_de_validacion_mapea_a_400()
    {
        var repository = new FakeOrdersTreeReadRepository();
        var handler = new GetOrdersTreeChildrenHandler(repository);

        var response = await handler.Handle(Request("nope"), CancellationToken.None);

        Assert.IsAssignableFrom<IValidationFailure>(response);
    }
}
