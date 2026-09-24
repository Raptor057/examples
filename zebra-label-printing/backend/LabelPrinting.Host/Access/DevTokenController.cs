using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace LabelPrinting.Host.Access;

/// <summary>
/// Emite un token para PROBAR el ejemplo. No es un sistema de identidad: no hay usuarios, no hay
/// contraseñas, y cualquiera puede pedir los permisos que quiera.
///
/// En un sistema real el token lo emite el servicio de identidad del ecosistema y esta clase NO
/// existe. Por eso solo se registra en Development: si alguien despliega esto a produccion tal
/// cual, el endpoint simplemente no esta.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/dev")]
public sealed class DevTokenController(IConfiguration configuration) : ControllerBase
{
    /// <param name="permissions">Separados por coma. Omitido = todos.</param>
    [HttpPost("token")]
    public IActionResult Issue([FromQuery] string? permissions = null)
    {
        var granted = string.IsNullOrWhiteSpace(permissions)
            ? PermissionCatalog.All
            : permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var desconocidos = granted.Except(PermissionCatalog.All).ToArray();
        if (desconocidos.Length > 0)
            return BadRequest(new { message = $"Permisos que no existen: {string.Join(", ", desconocidos)}." });

        var key = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Falta Jwt:SigningKey.");

        var claims = new List<Claim> { new(ClaimTypes.Name, "usuario-de-prueba") };
        claims.AddRange(granted.Select(p => new Claim(PermissionPolicies.PermissionClaim, p)));

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "zebra-label-printing",
            audience: configuration["Jwt:Audience"] ?? "zebra-label-printing-clients",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));

        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            permissions = granted,
        });
    }
}
