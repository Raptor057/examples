namespace Orders.Contracts;

/// <summary>
/// Unico tipo publico del modulo hacia afuera. Vive en Contracts, sin dependencias, porque es
/// la unica via por la que otro modulo puede saber algo de Orders: nadie referencia
/// Orders.Domain ni Orders.Application desde fuera.
///
/// Hoy el ejemplo tiene un solo modulo y nadie lo consume. Esta igual porque cuando aparece el
/// segundo modulo, la referencia directa ya esta puesta y cuesta el triple quitarla.
/// </summary>
public sealed record OrderSummary(
    Guid PublicId,
    string OrderNumber,
    string Status,
    DateTime PlacedAtUtc,
    decimal TotalAmount,
    int LineCount);
