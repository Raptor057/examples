using Automation.Application.UseCases.Tasks.RunTaskNow.Responses;
using Common.Messaging;

namespace Automation.Application.UseCases.Tasks.RunTaskNow;

/// <summary>
/// "Ejecutar ahora", para probar sin esperar al horario.
///
/// No lleva usuario: quien dispara sale del TOKEN. Si viniera por parametro, la bitacora
/// quedaria firmada por quien el cliente quisiera y dejaria de servir para lo unico que sirve.
/// </summary>
public sealed record RunTaskNowRequest(string? Code) : IRequest<RunTaskNowResponse>;
