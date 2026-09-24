using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Printing.Application.Services;
using Printing.Domain.Abstractions;
using Printing.Domain.Entities;
using Printing.Domain.Rules;
using Xunit;

namespace Printing.Tests;

public sealed class PrintQueueRulesTests
{
    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 30)]
    [InlineData(3, 120)]
    [InlineData(4, 480)]
    [InlineData(5, 1800)]
    [InlineData(99, 1800)]  // pasado el ultimo escalon se queda en el tope
    public void La_espera_entre_intentos_crece(int attempts, int expectedSeconds)
    {
        var now = new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc);
        Assert.Equal(now.AddSeconds(expectedSeconds), PrintQueueRules.NextAttemptAt(attempts, now));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(6, true)]
    public void Se_deja_de_reintentar_al_agotar_los_intentos(int attempts, bool exhausted)
        => Assert.Equal(exhausted, PrintQueueRules.IsExhausted(attempts));
}

public sealed class PreflightRulesTests
{
    private static PrinterStatusSnapshot Status(
        bool headOpen = false, bool paperOut = false, bool ribbonOut = false,
        bool headTooHot = false, bool paused = false, bool bufferFull = false) => new(
        "x", !headOpen && !paperOut, paperOut, headOpen, paused, ribbonOut, bufferFull, headTooHot, 0, "", DateTimeOffset.UtcNow);

    [Fact]
    public void Una_impresora_sana_no_bloquea()
        => Assert.Null(PreflightRules.BlockingReason(Status()));

    [Fact]
    public void El_buffer_lleno_NO_bloquea()
    {
        // Significa que esta ocupada imprimiendo, y se resuelve sola. Bloquear aqui rechazaria
        // etiquetas en medio de un lote, que es cuando mas se necesitan.
        Assert.Null(PreflightRules.BlockingReason(Status(bufferFull: true)));
    }

    [Fact]
    public void Con_el_cabezal_abierto_Y_sin_papel_se_reporta_el_cabezal_primero()
    {
        // Decir "sin papel" mandaria a alguien a cargar rollo en una impresora abierta.
        var reason = PreflightRules.BlockingReason(Status(headOpen: true, paperOut: true));
        Assert.Contains("cabezal", reason);
        Assert.Contains("abierto", reason);
    }

    [Theory]
    [InlineData(true, false, false, false, false)]
    [InlineData(false, true, false, false, false)]
    [InlineData(false, false, true, false, false)]
    [InlineData(false, false, false, true, false)]
    [InlineData(false, false, false, false, true)]
    public void Todo_lo_que_bloquea_es_temporal_y_por_lo_tanto_se_encola(
        bool headOpen, bool paperOut, bool ribbonOut, bool tooHot, bool paused)
    {
        var status = Status(headOpen, paperOut, ribbonOut, tooHot, paused);
        Assert.NotNull(PreflightRules.BlockingReason(status));
        Assert.True(PreflightRules.IsTemporary(status));
    }
}

public sealed class PrintQueueWorkerTests
{
    private readonly IPrintQueue _queue = Substitute.For<IPrintQueue>();
    private readonly IPrinterGateway _gateway = Substitute.For<IPrinterGateway>();
    private readonly IPrintJobLog _jobLog = Substitute.For<IPrintJobLog>();
    private readonly FakeTimeProvider _time = new();

    private PrintQueueWorker Worker()
        => new(_queue, _gateway, _jobLog, _time, NullLogger<PrintQueueWorker>.Instance);

    private static PrintQueueItem Item(int attempts = 1, string? targetJson = null) => new()
    {
        Id = 42,
        TargetJson = targetJson ?? JsonSerializer.Serialize(PrinterTarget.Network("192.168.0.50")),
        TargetLabel = "192.168.0.50:9100",
        Zpl = "^XA^XZ",
        Attempts = attempts,
        CreatedAtUtc = DateTime.UtcNow,
    };

    private void GivenDue(params PrintQueueItem[] items)
        => _queue.ClaimDueAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(items);

    [Fact]
    public async Task Un_trabajo_que_sale_se_marca_enviado_y_se_registra()
    {
        GivenDue(Item());
        _gateway.SendAsync(Arg.Any<PrinterTarget>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new PrintReceipt("x", 4, 1, DateTimeOffset.UtcNow));

        Assert.Equal(1, await Worker().RunOnceAsync(default));

        await _queue.Received(1).MarkSentAsync(42, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _jobLog.Received(1).RecordAsync(Arg.Is<PrintJobEntry>(e => e.Succeeded && e.QueueItemId == 42), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Un_fallo_que_aun_tiene_intentos_se_reprograma_y_NO_ensucia_la_bitacora()
    {
        GivenDue(Item(attempts: 1));
        _gateway.SendAsync(Arg.Any<PrinterTarget>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<PrintReceipt>(_ => throw new PrinterCommunicationException("sigue apagada"));

        Assert.Equal(0, await Worker().RunOnceAsync(default));

        await _queue.Received(1).MarkFailedAsync(42, 2, "sigue apagada", Arg.Any<DateTime>(), false, Arg.Any<CancellationToken>());

        // Anotar cada reintento llenaria la bitacora de ruido y esconderia las impresiones reales.
        await _jobLog.DidNotReceive().RecordAsync(Arg.Any<PrintJobEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Al_agotar_los_intentos_se_da_por_muerto_y_ESO_si_se_registra()
    {
        GivenDue(Item(attempts: PrintQueueRules.MaxAttempts - 1));
        _gateway.SendAsync(Arg.Any<PrinterTarget>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<PrintReceipt>(_ => throw new PrinterCommunicationException("nunca contesto"));

        await Worker().RunOnceAsync(default);

        await _queue.Received(1).MarkFailedAsync(
            42, PrintQueueRules.MaxAttempts, Arg.Any<string>(), Arg.Any<DateTime>(), true, Arg.Any<CancellationToken>());
        await _jobLog.Received(1).RecordAsync(
            Arg.Is<PrintJobEntry>(e => !e.Succeeded && e.Error!.Contains("agotaron")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Un_destino_ilegible_se_mata_de_una_sin_reintentar()
    {
        // Reintentar algo que ni siquiera se puede leer es gastar la cola para siempre.
        GivenDue(Item(targetJson: "esto no es json"));

        await Worker().RunOnceAsync(default);

        await _queue.Received(1).MarkFailedAsync(
            42, PrintQueueRules.MaxAttempts, Arg.Any<string>(), Arg.Any<DateTime>(), true, Arg.Any<CancellationToken>());
        await _gateway.DidNotReceive().SendAsync(Arg.Any<PrinterTarget>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sin_trabajos_vencidos_no_toca_la_impresora()
    {
        GivenDue();
        Assert.Equal(0, await Worker().RunOnceAsync(default));
        await _gateway.DidNotReceive().SendAsync(Arg.Any<PrinterTarget>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
