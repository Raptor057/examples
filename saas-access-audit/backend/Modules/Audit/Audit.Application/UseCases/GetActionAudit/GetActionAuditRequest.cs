using Audit.Application.UseCases.GetActionAudit.Responses;
using Common.Messaging;

namespace Audit.Application.UseCases.GetActionAudit;

/// <summary>
/// Filtros de la pantalla de bitacora. Todos son de PRESENTACION y por eso pueden llegar del
/// cliente; ninguno decide QUE datos se ven, solo cuales de los del tenant. El tenant sale del
/// token y no aparece aqui.
///
/// Las fechas llegan en UTC. El frontend es la unica capa que convierte a la hora local de quien
/// mira, en los dos sentidos: pinta en local y manda en UTC.
/// </summary>
public sealed record GetActionAuditRequest(
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? ActionCode,
    string? SubjectKey,
    string? PerformedBy,
    bool OnlyPendingExternalEffect,
    int? PageNumber,
    int? PageSize) : IRequest<GetActionAuditResponse>;
