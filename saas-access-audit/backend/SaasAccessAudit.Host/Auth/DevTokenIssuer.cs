using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SaasAccessAudit.Shared.Web;

namespace SaasAccessAudit.Host.Auth;

/// <summary>
/// Emisor de tokens del EJEMPLO. En un proyecto real esto es un servicio de identidad completo
/// (login, contrasenas, refresh, revocacion); aqui existe solo para poder entrar como una persona
/// u otra y ver en vivo quien puede y quien no.
///
/// Lo que SI es de verdad y hay que copiar:
///   - el tenant viaja FIRMADO en el token; el cliente elige credenciales, no que datos lee,
///   - el token trae PERTENENCIAS (grupos), NO permisos. Si trajera permisos, revocar uno
///     obligaria a esperar a que caduque la sesion.
/// </summary>
public sealed class DevTokenIssuer(IConfiguration configuration)
{
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? "saas-access-audit";
    private readonly string _audience = configuration["Jwt:Audience"] ?? "saas-access-audit-clients";
    private readonly SymmetricSecurityKey _signingKey = new(Encoding.UTF8.GetBytes(
        configuration["Jwt:SigningKey"]
        ?? throw new InvalidOperationException("Falta Jwt:SigningKey en la configuracion.")));

    public (string Token, DateTime ExpiresAtUtc) Issue(
        long tenantId, string tenantCode, string username, string displayName, IReadOnlyList<string> groups)
    {
        var expiresAtUtc = DateTime.UtcNow.AddHours(8);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, username),
            new(ClaimTypes.NameIdentifier, username),
            new(RequestContextMiddleware.DisplayNameClaim, displayName),
            new(RequestContextMiddleware.TenantClaim, tenantId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("tenant_code", tenantCode)
        };

        // El claim de pertenencia es REPETIBLE: una persona puede estar en varios grupos y sus
        // permisos son la union de lo que le dan todos ellos.
        claims.AddRange(groups.Select(group => new Claim(RequestContextMiddleware.GroupClaim, group)));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _issuer,
            Audience = _audience,
            Expires = expiresAtUtc,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expiresAtUtc);
    }
}
