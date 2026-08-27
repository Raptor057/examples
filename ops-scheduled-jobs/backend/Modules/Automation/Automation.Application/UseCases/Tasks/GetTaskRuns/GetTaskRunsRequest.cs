using Automation.Application.UseCases.Tasks.GetTaskRuns.Responses;
using Common.Messaging;

namespace Automation.Application.UseCases.Tasks.GetTaskRuns;

/// <summary>
/// La bitacora de UNA tarea, paginada. Pagina y tamano llegan del cliente porque son criterios
/// de PRESENTACION; el handler los valida y les pone tope (rules/server-side-data-boundaries).
/// </summary>
public sealed record GetTaskRunsRequest(string? Code, int? PageNumber, int? PageSize)
    : IRequest<GetTaskRunsResponse>;
