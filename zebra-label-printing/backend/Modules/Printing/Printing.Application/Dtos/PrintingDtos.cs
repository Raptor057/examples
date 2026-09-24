namespace Printing.Application.Dtos;

/// <summary>
/// Los DTO son el contrato con el cliente y NO son las entidades. Se separan a proposito: el dia
/// que la tabla gane una columna interna, el contrato HTTP no tiene por que enterarse.
/// </summary>
public sealed record PrinterDto(string Name, string Transport, string? Address, bool IsDefault);

public sealed record PrinterStatusDto(
    string Target,
    bool IsReadyToPrint,
    bool IsPaperOut,
    bool IsHeadOpen,
    bool IsPaused,
    bool IsRibbonOut,
    bool IsReceiveBufferFull,
    bool IsHeadTooHot,
    int LabelsRemainingInBatch,
    string Messages,
    DateTimeOffset ReadAtUtc);

public sealed record PrintReceiptDto(string Target, int BytesSent, long ElapsedMs, DateTimeOffset SentAtUtc);

/// <summary>
/// El acuse de una impresion. Dos campos merecen atencion:
///
/// <para><c>MissingValues</c>: la etiqueta SE IMPRIMIO, pero estos marcadores salieron en blanco.
/// Es un exito con advertencia, no un error.</para>
///
/// <para><c>Disposition</c>: <c>printed</c> o <c>queued</c>. NO son lo mismo y el cliente tiene que
/// distinguirlos: "encolada" significa que la impresora no contesto y se va a reintentar, no que la
/// etiqueta ya este pegada en la caja. Confundirlos es el bug clasico de una cola.</para>
/// </summary>
public sealed record TemplatePrintResultDto(
    PrintReceiptDto? Receipt,
    string? TemplateCode,
    int? Dpi,
    int? TemplateVersion,
    IReadOnlyList<string> MissingValues,
    string Disposition,
    long? QueueItemId,
    string? Message);

public sealed record TemplateVersionDto(
    int Version,
    string Name,
    string? Description,
    string Body,
    DateTime CreatedAtUtc,
    DateTime? ReplacedAtUtc);

public sealed record PrintQueueItemDto(
    long Id,
    string Target,
    string? TemplateCode,
    int? Dpi,
    string Status,
    int Attempts,
    DateTime NextAttemptAtUtc,
    string? LastError,
    DateTime CreatedAtUtc);

public sealed record TemplateDto(
    int Id,
    string Code,
    string Name,
    string? Description,
    string Body,
    int Dpi,
    int Version,
    bool IsActive,
    IReadOnlyList<string> Placeholders,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record PrintJobDto(
    long Id,
    string Target,
    string? TemplateCode,
    int? Dpi,
    string Zpl,
    bool Succeeded,
    string? Error,
    DateTime CreatedAtUtc,
    int? TemplateVersion,
    long? QueueItemId);

/// <summary>A donde imprimir, tal como llega del cliente. El mapeo a dominio valida la forma.</summary>
public sealed record PrinterTargetDto(string Transport, string? Address, int? Port, string? QueueName);
