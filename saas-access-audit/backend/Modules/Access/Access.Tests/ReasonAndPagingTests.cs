using Access.Application;
using Access.Application.UseCases.GetUsers;
using Access.Application.UseCases.GetUsers.Responses;
using SaasAccessAudit.Kernel;

namespace Access.Tests;

/// <summary>
/// Dos defensas pequenas que se rompen sin dar error: el motivo vacio y el tamano de pagina sin
/// tope. Ninguna de las dos lanza una excepcion cuando falla; una llena la bitacora de renglones
/// inutiles y la otra convierte una pantalla en una descarga completa de la tabla.
/// </summary>
public sealed class ReasonAndPagingTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("\t\n")]
    [InlineData("no")]
    public void Un_motivo_que_no_dice_nada_se_rechaza(string? raw)
    {
        var (reason, error) = ReasonPolicy.Normalize(raw);

        Assert.Null(reason);
        Assert.NotNull(error);
    }

    [Fact]
    public void El_motivo_se_recorta_antes_de_medirse()
    {
        // Cinco espacios no son un motivo, aunque midan cinco caracteres.
        var (reason, error) = ReasonPolicy.Normalize("   Baja voluntaria.   ");

        Assert.Null(error);
        Assert.Equal("Baja voluntaria.", reason);
    }

    [Fact]
    public void Un_motivo_desmedido_tampoco_pasa()
    {
        var (reason, error) = ReasonPolicy.Normalize(new string('a', ReasonPolicy.MaxLength + 1));

        Assert.Null(reason);
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData(null, Paging.DefaultPageSize)]
    [InlineData(0, Paging.DefaultPageSize)]
    [InlineData(-5, Paging.DefaultPageSize)]
    [InlineData(10, 10)]
    [InlineData(1000, Paging.MaxPageSize)]
    public void El_tamano_de_pagina_siempre_tiene_tope(int? requested, int expected) =>
        Assert.Equal(expected, Paging.NormalizePageSize(requested));

    [Theory]
    [InlineData(null, 1)]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(4, 4)]
    public void La_pagina_nunca_es_menor_que_uno(int? requested, int expected) =>
        Assert.Equal(expected, Paging.NormalizePageNumber(requested));

    [Fact]
    public void El_desplazamiento_se_calcula_fuera_del_SQL() =>
        Assert.Equal(75, Paging.Offset(4, 25));

    [Fact]
    public async Task El_handler_aplica_el_tope_antes_de_llegar_a_la_base()
    {
        // La normalizacion vive en el handler y no en el controller: un cliente nuevo que llame
        // el caso de uso por otra via sigue teniendo el tope puesto.
        var repository = new FakeAccessReadRepository();
        var handler = new GetUsersHandler(repository);

        var response = await handler.Handle(
            new GetUsersRequest(null, false, PageNumber: 1, PageSize: 100_000), CancellationToken.None);

        var success = Assert.IsType<GetUsersSuccess>(response);
        Assert.Equal(Paging.MaxPageSize, success.Data.PageSize);
    }
}
