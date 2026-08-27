using System.Text;
using Common.Messaging;
using LazyHierarchy.Host.Auth;
using LazyHierarchy.Shared;
using LazyHierarchy.Shared.Persistence;
using LazyHierarchy.Shared.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Orders.Application;
using Orders.Infrastructure;
using Orders.Infrastructure.Seeding;
using Orders.Presentation;

var builder = WebApplication.CreateBuilder(args);

// --- Composicion ---------------------------------------------------------------------------
// El Host es composition root: aqui no hay logica de negocio, solo cableado. Todo se registra a
// mano; no hay escaneo de ensamblados en ninguna capa.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Los nulos no viajan. En un arbol donde cada nodo declara cinco coordenadas y cinco
    // metricas de las que casi siempre usa dos, omitirlos recorta la respuesta a la mitad, y el
    // peso por nivel es justo el argumento del patron.
    options.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Capa de datos compartida: Dapper (conexion tipada por marcador) + EF Core (DbContext unico).
// Los dos conviven porque el proyecto es multi-tenant y a la vez necesita SQL a mano para el
// arbol; el README explica el precio de esa mezcla y como se paga.
builder.Services.AddSharedPostgresInfrastructure(
    builder.Configuration,
    Orders.Infrastructure.ServiceCollectionEx.ModelConfigurationAssembly);

// Sin esta linea el proyecto compila, los controllers existen y toda peticion devuelve 500.
builder.Services.AddMediator();

builder.Services
    .AddOrdersApplicationServices()
    .AddOrdersInfrastructureServices()
    .AddOrdersPresentationServices();

builder.Services.AddSingleton<DevTokenIssuer>();

var signingKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Falta Jwt:SigningKey en la configuracion.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "lazy-hierarchy-orders",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "lazy-hierarchy-orders-clients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

const string DevCorsPolicy = "dev-webclient";
builder.Services.AddCors(options => options.AddPolicy(DevCorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// --- Esquema y datos -----------------------------------------------------------------------
// MigrateAsync y no EnsureCreated: EnsureCreated no ejecuta migraciones ni altera tablas
// existentes, solo sirve para prototipos desechables.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // "dotnet run -- seed" siembra y termina: el volumen tarda minutos y no tiene por que
    // correr cada vez que alguien levanta la API.
    if (args.Contains("seed", StringComparer.OrdinalIgnoreCase))
    {
        var seeder = scope.ServiceProvider.GetRequiredService<OrdersSeeder>();
        await seeder.SeedAsync();
        return;
    }

    // "dotnet run -- migrate" solo aplica el esquema y termina.
    if (args.Contains("migrate", StringComparer.OrdinalIgnoreCase))
        return;
}

// --- Pipeline ------------------------------------------------------------------------------
app.UseCors(DevCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

// DESPUES de UseAuthentication, siempre: antes, HttpContext.User no tiene claims todavia y el
// tenant quedaria vacio, con lo que todo filtraria contra el contexto sistema y "no hay datos".
app.UseMiddleware<TenantClaimsMiddleware>();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", utcTimeStamp = DateTime.UtcNow }))
    .AllowAnonymous();

await app.RunAsync();
