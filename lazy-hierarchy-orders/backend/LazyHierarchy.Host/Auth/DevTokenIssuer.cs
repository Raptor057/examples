using System.Security.Claims;
using System.Text;
using LazyHierarchy.Shared.Web;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LazyHierarchy.Host.Auth;

/// <summary>
/// Emisor de tokens del EJEMPLO. En un proyecto real esto es un servicio de identidad completo
/// (login, contrasenas, refresh, revocacion); aqui existe solo para que el arbol tenga un token
/// con el claim de tenant, que es lo unico que el patron necesita demostrar.
///
/// Lo que SI es de verdad y hay que copiar: el tenant vive en el token, firmado por el servidor.
/// El cliente puede elegir con que credenciales entra, nunca que tenant lee.
/// </summary>
public sealed class DevTokenIssuer(IConfiguration configuration)
{
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? "lazy-hierarchy-orders";
    private readonly string _audience = configuration["Jwt:Audience"] ?? "lazy-hierarchy-orders-clients";
    private readonly SymmetricSecurityKey _signingKey = new(Encoding.UTF8.GetBytes(
        configuration["Jwt:SigningKey"]
        ?? throw new InvalidOperationException("Falta Jwt:SigningKey en la configuracion.")));

    public (string Token, DateTime ExpiresAtUtc) Issue(long tenantId, string tenantCode)
    {
        var expiresAtUtc = DateTime.UtcNow.AddHours(8);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _issuer,
            Audience = _audience,
            Expires = expiresAtUtc,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, $"demo-user@{tenantCode}"),
                new Claim(TenantClaimsMiddleware.TenantClaim, tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim("tenant_code", tenantCode)
            ]),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expiresAtUtc);
    }
}
