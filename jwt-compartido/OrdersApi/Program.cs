using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuracion JWT (LA MISMA clave que AuthApi) ----
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Falta configuracion Jwt:Key");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

// ---- Esta API NO emite tokens: solo valida los que vienen firmados con la clave compartida ----
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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Orders API (solo valida)", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega el token emitido por AuthApi (/login en el puerto 5001)."
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

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Orders API v1");
    c.DocumentTitle = "Orders API - Swagger";
});

app.UseAuthentication();
app.UseAuthorization();

// ---- Endpoint protegido: acepta el token emitido por AuthApi ----
app.MapGet("/orders", (ClaimsPrincipal user) =>
{
    return Results.Ok(new
    {
        api = "OrdersApi",
        mensaje = $"Token aceptado. Hola {user.Identity?.Name}, estas viendo pedidos protegidos.",
        pedidos = new[]
        {
            new { id = 1, cliente = "ACME", total = 1500.00m },
            new { id = 2, cliente = "Globex", total = 980.50m }
        }
    });
})
.RequireAuthorization()
.WithName("Orders")
.WithSummary("Endpoint protegido. Requiere el JWT emitido por AuthApi.");

app.MapGet("/", () => Results.Ok(new { api = "OrdersApi", status = "ok", swagger = "/swagger" }));

app.Run();
