using NSubstitute;
using Printing.Application.Dtos;
using Printing.Application.Services;
using Printing.Application.UseCases.Templates;
using Printing.Domain.Abstractions;
using Printing.Domain.Entities;
using Xunit;

namespace Printing.Tests;

/// <summary>
/// El caso de uso central probado con los puertos sustituidos: ni base ni impresora. Lo que se
/// verifica no es que "funcione", sino las decisiones que un refactor podria deshacer sin que
/// nadie se entere.
/// </summary>
public sealed class PrintTemplateHandlerTests
{
    private readonly ILabelTemplateRepository _repository = Substitute.For<ILabelTemplateRepository>();
    private readonly IPrinterGateway _gateway = Substitute.For<IPrinterGateway>();
    private readonly IPrintQueue _queue = Substitute.For<IPrintQueue>();
    private readonly IPrintJobLog _jobLog = Substitute.For<IPrintJobLog>();
    private readonly FakeTimeProvider _time = new();

    private PrintTemplateHandler Handler()
        => new(_repository, new PrintDispatchService(_gateway, _queue, _jobLog, _time));

    private static LabelTemplate Template(string body = "^XA^FD{{SERIAL}}^FS^XZ", bool active = true, int version = 1) => new()
    {
        Id = 1,
        Code = "BOX_LABEL",
        Name = "Caja",
        Body = body,
        Dpi = 203,
        Version = version,
        IsActive = active,
        CreatedAtUtc = DateTime.UtcNow,
    };

    private static PrinterTargetDto NetworkTarget() => new("network", "192.168.0.50", 9100, null);

    private void GivenTemplate(LabelTemplate template)
        => _repository.FindAsync(template.Code, template.Dpi, Arg.Any<CancellationToken>()).Returns(template);

    private void GivenPrinterAccepts()
    {
        _gateway.SupportsStatus.Returns(false);
        _gateway.SendAsync(Arg.Any<PrinterTarget>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new PrintReceipt("192.168.0.50:9100", 10, 5, DateTimeOffset.UtcNow));
    }

    private void GivenPrinterUnreachable()
    {
        _gateway.SupportsStatus.Returns(false);
        _gateway.SendAsync(Arg.Any<PrinterTarget>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<PrintReceipt>(_ => throw new PrinterCommunicationException("La impresora no responde."));
    }

    [Fact]
    public async Task Imprime_el_ZPL_con_los_valores_ya_sustituidos()
    {
        GivenTemplate(Template());
        GivenPrinterAccepts();

        var response = await Handler().Handle(
            new PrintTemplateRequest("box_label", 203, NetworkTarget(),
                new Dictionary<string, string?> { ["serial"] = "GT-001" }),
            default);

        var success = Assert.IsType<PrintTemplateSuccess>(response);
        Assert.Equal("printed", success.Data.Disposition);

        // La impresora recibe el ZPL RENDERIZADO, nunca la plantilla con marcadores. Y la llave
        // llego en minusculas: se normaliza aqui, no lo hace quien llama.
        await _gateway.Received(1).SendAsync(
            Arg.Any<PrinterTarget>(),
            Arg.Is<string>(zpl => zpl.Contains("GT-001") && !zpl.Contains("{{")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Un_valor_que_falta_es_advertencia_y_NO_impide_imprimir()
    {
        GivenTemplate(Template("^XA^FD{{SERIAL}}{{LOTE}}^FS^XZ"));
        GivenPrinterAccepts();

        var response = await Handler().Handle(
            new PrintTemplateRequest("BOX_LABEL", 203, NetworkTarget(),
                new Dictionary<string, string?> { ["SERIAL"] = "GT-001" }),
            default);

        var success = Assert.IsType<PrintTemplateSuccess>(response);
        Assert.Equal(["LOTE"], success.Data.MissingValues);
        Assert.Equal("printed", success.Data.Disposition);
    }

    [Fact]
    public async Task Si_la_impresora_no_contesta_el_trabajo_SE_ENCOLA_y_no_se_pierde()
    {
        GivenTemplate(Template());
        GivenPrinterUnreachable();
        _queue.EnqueueAsync(Arg.Any<PrintQueueItem>(), Arg.Any<CancellationToken>()).Returns(77L);

        var response = await Handler().Handle(
            new PrintTemplateRequest("BOX_LABEL", 203, NetworkTarget(), null), default);

        // Es EXITO con matiz, no un fallo: la etiqueta sigue viva y se va a reintentar. Decir 409
        // aqui haria que el cliente la diera por perdida y la volviera a mandar.
        var success = Assert.IsType<PrintTemplateSuccess>(response);
        Assert.Equal("queued", success.Data.Disposition);
        Assert.Equal(77L, success.Data.QueueItemId);

        // Lo que se encola es el ZPL ya renderizado: si la plantilla cambia mientras espera, la
        // etiqueta que salga tiene que ser la que se pidio.
        await _queue.Received(1).EnqueueAsync(
            Arg.Is<PrintQueueItem>(i => !i.Zpl.Contains("{{") && i.Attempts == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Con_queueOnFailure_apagado_una_impresora_caida_es_conflicto()
    {
        GivenTemplate(Template());
        GivenPrinterUnreachable();

        var response = await Handler().Handle(
            new PrintTemplateRequest("BOX_LABEL", 203, NetworkTarget(), null, QueueOnFailure: false), default);

        Assert.IsType<PrintTemplateUnreachable>(response);
        await _queue.DidNotReceive().EnqueueAsync(Arg.Any<PrintQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task El_preflight_detiene_el_envio_si_la_impresora_no_esta_en_condiciones()
    {
        GivenTemplate(Template());
        _gateway.SupportsStatus.Returns(true);
        _gateway.GetStatusAsync(Arg.Any<PrinterTarget>(), Arg.Any<CancellationToken>())
            .Returns(Status(paperOut: true));
        _queue.EnqueueAsync(Arg.Any<PrintQueueItem>(), Arg.Any<CancellationToken>()).Returns(5L);

        var response = await Handler().Handle(
            new PrintTemplateRequest("BOX_LABEL", 203, NetworkTarget(), null), default);

        // No se mandan los bytes: entrarian al buffer de una impresora sin papel, la API diria
        // que si, y la etiqueta no existiria.
        await _gateway.DidNotReceive().SendAsync(Arg.Any<PrinterTarget>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        // Y como se arregla solo en cuanto alguien carga el rollo, se encola.
        var success = Assert.IsType<PrintTemplateSuccess>(response);
        Assert.Equal("queued", success.Data.Disposition);
        Assert.Contains("etiquetas", success.Data.Message!);
    }

    [Fact]
    public async Task Se_puede_reimprimir_una_version_vieja_con_SU_cuerpo()
    {
        GivenTemplate(Template(body: "^XA^FDNUEVO {{SERIAL}}^FS^XZ", version: 3));
        _repository.FindVersionAsync("BOX_LABEL", 203, 1, Arg.Any<CancellationToken>()).Returns(
            new LabelTemplateVersion
            {
                Code = "BOX_LABEL", Dpi = 203, Version = 1, Name = "Caja",
                Body = "^XA^FDVIEJO {{SERIAL}}^FS^XZ", CreatedAtUtc = DateTime.UtcNow,
            });
        GivenPrinterAccepts();

        var response = await Handler().Handle(
            new PrintTemplateRequest("BOX_LABEL", 203, NetworkTarget(),
                new Dictionary<string, string?> { ["SERIAL"] = "GT-001" }, Version: 1),
            default);

        var success = Assert.IsType<PrintTemplateSuccess>(response);
        Assert.Equal(1, success.Data.TemplateVersion);
        await _gateway.Received(1).SendAsync(
            Arg.Any<PrinterTarget>(), Arg.Is<string>(z => z.Contains("VIEJO")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Una_plantilla_dada_de_baja_no_se_imprime()
    {
        GivenTemplate(Template(active: false));

        var response = await Handler().Handle(
            new PrintTemplateRequest("BOX_LABEL", 203, NetworkTarget(), null), default);

        Assert.IsType<PrintTemplateInvalid>(response);
        await _gateway.DidNotReceive().SendAsync(Arg.Any<PrinterTarget>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Si_no_existe_para_ESA_resolucion_el_mensaje_nombra_las_dos_partes_de_la_llave()
    {
        _repository.FindAsync("BOX_LABEL", 300, Arg.Any<CancellationToken>()).Returns((LabelTemplate?)null);

        var response = await Handler().Handle(
            new PrintTemplateRequest("BOX_LABEL", 300, NetworkTarget(), null), default);

        var notFound = Assert.IsType<PrintTemplateNotFound>(response);
        Assert.Contains("BOX_LABEL", notFound.Message);
        Assert.Contains("300", notFound.Message);
    }

    [Theory]
    [InlineData(null, null, null, null)]
    [InlineData("network", null, null, null)]
    [InlineData("installed", null, null, null)]
    [InlineData("carrier-pigeon", "x", null, null)]
    public async Task Un_destino_mal_formado_es_400_y_no_una_excepcion(
        string? transport, string? address, int? port, string? queue)
    {
        var target = transport is null ? null : new PrinterTargetDto(transport, address, port, queue);

        var response = await Handler().Handle(
            new PrintTemplateRequest("BOX_LABEL", 203, target, null), default);

        Assert.IsType<PrintTemplateInvalid>(response);
    }

    private static PrinterStatusSnapshot Status(bool paperOut = false, bool headOpen = false) => new(
        "192.168.0.50:9100",
        IsReadyToPrint: !paperOut && !headOpen,
        IsPaperOut: paperOut,
        IsHeadOpen: headOpen,
        IsPaused: false,
        IsRibbonOut: false,
        IsReceiveBufferFull: false,
        IsHeadTooHot: false,
        LabelsRemainingInBatch: 0,
        Messages: string.Empty,
        ReadAtUtc: DateTimeOffset.UtcNow);
}

/// <summary>
/// Reloj de mentira. El tiempo es una dependencia como cualquier otra: una prueba que dependa del
/// reloj de verdad falla sola un martes a medianoche.
/// </summary>
internal sealed class FakeTimeProvider(DateTimeOffset? start = null) : TimeProvider
{
    private DateTimeOffset _now = start ?? new DateTimeOffset(2026, 1, 15, 8, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}
