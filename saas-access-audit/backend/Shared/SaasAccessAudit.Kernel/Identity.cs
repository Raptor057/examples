namespace SaasAccessAudit.Kernel;

/// <summary>
/// Identidad de la peticion en curso, tal como viene FIRMADA en el token.
///
/// Lo que trae y lo que NO trae es la mitad del patron de control de acceso:
///   - trae <see cref="Groups"/>, que son las PERTENENCIAS de la persona,
///   - no trae permisos.
///
/// Si el token trajera permisos, revocar uno exigiria esperar a que caduque la sesion: la
/// persona seguiria entrando con el permiso viejo hasta entonces. Con pertenencias, el permiso
/// efectivo se recalcula en cada peticion y un cambio de rol surte efecto de inmediato.
/// </summary>
public sealed record UserContext(
    string Username,
    string DisplayName,
    IReadOnlyList<string> Groups);

/// <summary>
/// Unica fuente de la identidad actual. La llena el middleware a partir de los claims del token;
/// nunca se alimenta de la query string ni del cuerpo de la peticion. Si el usuario llegara por
/// parametro, cualquiera actuaria en nombre de otro y la bitacora registraria a la victima.
/// </summary>
public interface IUserContextAccessor
{
    UserContext? Current { get; set; }
}
