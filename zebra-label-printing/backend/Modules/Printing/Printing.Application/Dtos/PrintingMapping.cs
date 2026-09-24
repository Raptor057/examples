using Printing.Domain.Abstractions;
using Printing.Domain.Entities;
using Printing.Domain.Rules;

namespace Printing.Application.Dtos;

public static class PrintingMapping
{
    public static PrinterDto ToDto(this DiscoveredPrinter printer)
        => new(printer.Name, printer.Transport.ToString().ToLowerInvariant(), printer.Address, printer.IsDefault);

    public static PrinterStatusDto ToDto(this PrinterStatusSnapshot s)
        => new(s.Target, s.IsReadyToPrint, s.IsPaperOut, s.IsHeadOpen, s.IsPaused, s.IsRibbonOut,
               s.IsReceiveBufferFull, s.IsHeadTooHot, s.LabelsRemainingInBatch, s.Messages, s.ReadAtUtc);

    public static PrintReceiptDto ToDto(this PrintReceipt r)
        => new(r.Target, r.BytesSent, r.ElapsedMs, r.SentAtUtc);

    public static TemplateDto ToDto(this LabelTemplate t)
        => new(t.Id, t.Code, t.Name, t.Description, t.Body, t.Dpi, t.Version, t.IsActive,
               TemplateRules.Placeholders(t.Body), t.CreatedAtUtc, t.UpdatedAtUtc);

    public static PrintJobDto ToDto(this PrintJobEntry e)
        => new(e.Id, e.Target, e.TemplateCode, e.Dpi, e.Zpl, e.Succeeded, e.Error, e.CreatedAtUtc,
               e.TemplateVersion, e.QueueItemId);

    /// <summary>
    /// Convierte el destino que mando el cliente. Devuelve el motivo del rechazo en vez de lanzar:
    /// un destino mal escrito es un 400, no una excepcion.
    /// </summary>
    public static (PrinterTarget? Target, string? Error) ToDomain(this PrinterTargetDto? dto)
    {
        if (dto is null) return (null, "Falta el destino de impresion.");

        var transport = (dto.Transport ?? string.Empty).Trim().ToLowerInvariant();
        switch (transport)
        {
            case "network":
                if (string.IsNullOrWhiteSpace(dto.Address))
                    return (null, "Para imprimir por red hace falta la direccion de la impresora.");
                if (dto.Port is not null && (dto.Port < 1 || dto.Port > 65535))
                    return (null, "El puerto debe estar entre 1 y 65535.");
                return (PrinterTarget.Network(dto.Address.Trim(), dto.Port), null);

            case "installed":
                if (string.IsNullOrWhiteSpace(dto.QueueName))
                    return (null, "Para imprimir en una cola instalada hace falta su nombre.");
                return (PrinterTarget.Installed(dto.QueueName.Trim()), null);

            default:
                return (null, "El transporte debe ser 'network' o 'installed'.");
        }
    }
}
