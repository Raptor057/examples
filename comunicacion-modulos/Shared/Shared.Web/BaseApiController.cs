using Common.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace Shared.Web;

// Controller base del chasis:
// expone el IMediator a los controllers de cada modulo. La ruta la define cada
// controller concreto.
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected readonly IMediator Mediator;

    protected BaseApiController(IMediator mediator) => Mediator = mediator;
}
