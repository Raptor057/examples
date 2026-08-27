using Automation.Application.UseCases.Tasks.SetTaskEnabled.Responses;
using Common.Messaging;

namespace Automation.Application.UseCases.Tasks.SetTaskEnabled;

/// <summary>
/// Pausar o reanudar SIN desplegar. Es la mitad del trato del patron: el codigo decide que
/// tareas existen, la base decide como estan puestas.
///
/// El motivo se pide al pausar y se GUARDA. Pedirlo y tirarlo no sirve de nada: dentro de dos
/// semanas, la pregunta que alguien va a hacer es por que esta apagada, no cuando se apago.
/// </summary>
public sealed record SetTaskEnabledRequest(string? Code, bool IsEnabled, string? Reason)
    : IRequest<SetTaskEnabledResponse>;
