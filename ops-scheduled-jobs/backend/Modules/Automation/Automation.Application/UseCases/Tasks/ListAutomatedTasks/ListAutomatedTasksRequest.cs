using Automation.Application.UseCases.Tasks.ListAutomatedTasks.Responses;
using Common.Messaging;

namespace Automation.Application.UseCases.Tasks.ListAutomatedTasks;

/// <summary>
/// La lista de tareas no lleva parametros: son tres, las declara el codigo y caben en pantalla.
/// Filtrar o paginar aqui seria complicar lo que no lo pide (la bitacora si pagina, y por eso
/// esa si los lleva).
/// </summary>
public sealed record ListAutomatedTasksRequest : IRequest<ListAutomatedTasksResponse>;
