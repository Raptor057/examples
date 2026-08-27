namespace Automation.Domain.Entities;

/// <summary>Un evento de pedido: la materia prima de refresh-order-summary.</summary>
public sealed record OrderEventRecord(
    long Id,
    DateTime OccurredAtUtc,
    string OrderNumber,
    string ChannelCode,
    int Units,
    decimal Amount);

/// <summary>Un temporal candidato a purga.</summary>
public sealed record TempFileRecord(
    long Id,
    string FileName,
    long SizeBytes,
    DateTime ExpiresAtUtc);

/// <summary>Un envio encolado hacia un sistema ajeno.</summary>
public sealed record PendingDispatchRecord(
    long Id,
    string Reference,
    DateTime QueuedAtUtc,
    int Attempts,
    string SimulatedOutcome);
