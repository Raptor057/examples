using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuracion JWT (compartida entre las 3 APIs) ----
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta configuracion Jwt:Key");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

// ---- Autenticacion: valida tokens firmados con la clave compartida ----
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5)
        };
    });
builder.Services.AddAuthorization();

// ---- Swagger con boton Authorize (Bearer) ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API (emisor del token)", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega aqui SOLO el token (sin la palabra Bearer)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Swagger habilitado siempre (es una demo)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API v1");
    c.DocumentTitle = "Auth API - Swagger";
});

app.UseAuthentication();
app.UseAuthorization();

// Usuarios demo (login minimo, en memoria)
var usuarios = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["admin"] = "admin123",
    ["rogelio"] = "demo"
};

// ---- LOGIN: unico endpoint que emite el token ----
app.MapPost("/login", (LoginRequest req) =>
{
    if (!usuarios.TryGetValue(req.Username, out var pass) || pass != req.Password)
        return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, req.Username),
        new Claim(ClaimTypes.Name, req.Username),
        new Claim(ClaimTypes.Role, "user"),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: creds);

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new
    {
        access_token = jwt,
        token_type = "Bearer",
        expires_in = 3600,
        nota = "Este mismo token sirve en OrdersApi (5002) y CatalogApi (5003)."
    });
})
.WithName("Login")
.WithSummary("Login minimo. Devuelve el JWT compartido. Prueba admin/admin123.");

// ---- Endpoint protegido propio de esta API ----
app.MapGet("/me", (ClaimsPrincipal user) =>
{
    return Results.Ok(new
    {
        api = "AuthApi",
        usuario = user.Identity?.Name,
        claims = user.Claims.Select(c => new { c.Type, c.Value })
    });
})
.RequireAuthorization()
.WithName("Me")
.WithSummary("Endpoint protegido: devuelve los claims del token.");

app.MapGet("/", () => Results.Ok(new { api = "AuthApi", status = "ok", swagger = "/swagger" }));

app.Run();

record LoginRequest(string Username, string Password);
