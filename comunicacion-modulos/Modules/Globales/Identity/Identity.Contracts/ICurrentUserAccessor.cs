namespace Identity.Contracts;

// Contrato global: cualquier modulo que necesite saber "quien hace
// esta request" depende de esta interfaz, nunca de como se implementa.
public interface ICurrentUserAccessor
{
    bool IsAuthenticated { get; }
    string UserId { get; }
    string Nombre { get; }
    string Rol { get; }
}
