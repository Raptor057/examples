using Identity.Contracts;
using Microsoft.AspNetCore.Http;

namespace Identity.Infrastructure;

// En ArccNova lee los claims del JWT desde HttpContext. Aqui, para la demo sin
// auth real, lee headers (X-User-*) y si no vienen usa un usuario demo. La idea
// es la misma: el accessor traduce el contexto del request al contrato.
public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserAccessor(IHttpContextAccessor accessor) => _accessor = accessor;

    private string? Header(string name)
    {
        var value = _accessor.HttpContext?.Request.Headers[name].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public bool IsAuthenticated => Header("X-User-Id") is not null;

    public string UserId => Header("X-User-Id") ?? "0";

    public string Nombre => Header("X-User-Name") ?? "Usuario Demo";

    public string Rol => Header("X-User-Role") ?? "Vendedor";
}
