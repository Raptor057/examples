using System.Text;
using Access.Application;
using Access.Infrastructure;
using Access.Infrastructure.Seeding;
using Access.Presentation;
using Audit.Application;
using Audit.Infrastructure;
using Audit.Presentation;
using Common.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SaasAccessAudit.Host.Auth;
using SaasAccessAudit.Host.Seeding;
using SaasAccessAudit.Shared;
using SaasAccessAudit.Shared.Persistence;
using SaasAccessAudit.Shared.Web;

var builder = WebApplication.CreateBuilder(args);

// --- Composicion ---------------------------------------------------------------------------
// El Host es composition root: aqui no hay logica de negocio, solo cableado. Todo se registra a
// mano; no hay escaneo de ensamblados en ninguna capa.

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull);

// Capa de datos compartida: EF Core (DbContext unico con los filtros de tenant) + Dapper
// (conexion tipada por marcador, para las lecturas pesadas de la bitacora). Los dos modulos
// aportan sus IEntityTypeConfiguration.
builder.Services.AddSharedPostgresInfrastructure(
    builder.Configuration,
    Access.Infrastructure.ServiceCollectionEx.ModelConfigurationAssembly,
    Audit.Infrastructure.ServiceCollectionEx.ModelConfigurationAssembly);

// Sin esta linea el proyecto compila, los controllers existen y toda peticion devuelve 500.
builder.Services.AddMediator();

builder.Services
    .AddAccessApplicationServices()
    .AddAccessInfrastructureServices(builder.Configuration)
    .AddAccessPresentationServices()
    .AddAuditApplicationServices()
    .AddAuditInfrastructureServices()
    .AddAuditPresentationServices();

builder.Services.AddSingleton<DevTokenIssuer>();
builder.Services.AddScoped<DemoSeeder>();

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
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "saas-access-audit",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "saas-access-audit-clients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

const string DevCorsPolicy = "dev-webclient";
builder.Services.AddCors(options => options.AddPolicy(DevCorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5190"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// --- Esquema, catalogo y datos ---------------------------------------------------------------
await using (var scope = app.Services.CreateAsyncScope())
{
    // MigrateAsync y no EnsureCreated: EnsureCreated no ejecuta migraciones ni altera tablas
    // existentes, solo sirve para prototipos desechables.
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // EL CATALOGO DE PERMISOS SE SIEMBRA EN CADA ARRANQUE. Esto no es parte de la demostracion:
    // es parte del patron, y en produccion corre igual. Agregar un permiso es una linea de
    // codigo, no un script que alguien tiene que acordarse de correr en cada ambiente.
    await scope.ServiceProvider.GetRequiredService<PermissionCatalogSeeder>().SyncAsync();

    // "dotnet run -- seed" siembra los datos de demostracion y termina.
    if (args.Contains("seed", StringComparer.OrdinalIgnoreCase))
    {
        await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync();
        return;
    }

    // "dotnet run -- migrate" aplica esquema y catalogo, y termina.
    if (args.Contains("migrate", StringComparer.OrdinalIgnoreCase))
        return;
}

// --- Pipeline ------------------------------------------------------------------------------
app.UseCors(DevCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

// DESPUES de UseAuthentication, siempre: antes, HttpContext.User no tiene claims todavia, el
// tenant quedaria vacio -y todo filtraria contra el contexto sistema- y cada renglon de bitacora
// diria "anonimo".
app.UseMiddleware<RequestContextMiddleware>();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", utcTimeStamp = DateTime.UtcNow }))
    .AllowAnonymous();

await app.RunAsync();
