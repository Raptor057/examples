using System.Text;
using Common.Messaging;
using LabelPrinting.Host;
using LabelPrinting.Host.Access;
using LabelPrinting.Shared;
using LabelPrinting.Shared.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Printing.Application;
using Printing.Domain.Abstractions;
using Printing.Infrastructure;
using Printing.Presentation;

var builder = WebApplication.CreateBuilder(args);

// --- Composicion -----------------------------------------------------------------------------
// El Host es composition root y nada mas: aqui no hay una sola regla de negocio, solo cableado.
// Todo se registra a mano; no hay escaneo de ensamblados en ninguna capa.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition =
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddOpenApi();

// Sin esta linea el proyecto compila, los controllers existen y toda peticion devuelve 500.
builder.Services.AddMediator();

builder.Services.AddSharedPersistence();
builder.Services
    .AddPrintingApplication()
    .AddPrintingInfrastructure(builder.Configuration)
    .AddPrintingPresentation();

// --- Autenticacion y permisos ------------------------------------------------------------------
var signingKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Falta Jwt:SigningKey en la configuracion.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "zebra-label-printing",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "zebra-label-printing-clients",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        ClockSkew = TimeSpan.FromMinutes(1),
    });

// La bandera permite encender permisos en un sistema que YA tiene consumidores, sin romperlos el
// mismo dia: en false las politicas existen y los endpoints las declaran, pero dejan pasar.
var enforcePermissions = builder.Configuration.GetValue("Access:EnforcePermissions", true);
builder.Services.AddPermissionPolicies(enforcePermissions);

const string DevCorsPolicy = "dev-webclient";
builder.Services.AddCors(options => options.AddPolicy(DevCorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

// El que mueve la cola. Va aqui y no en un modulo porque es hospedaje, no negocio.
builder.Services.AddHostedService<PrintQueueDispatcher>();

var app = builder.Build();

// --- Esquema ---------------------------------------------------------------------------------
// En un proyecto de verdad esto NO existe: los scripts los aplica una persona y el arranque de una
// API no deberia poder tocar el esquema. Aqui esta para que el ejemplo se levante con un comando.
// Ver ADR-0005.
await using (var scope = app.Services.CreateAsyncScope())
{
    var bootstrapper = scope.ServiceProvider.GetRequiredService<SchemaBootstrapper>();
    await bootstrapper.ApplyAsync(Path.Combine(AppContext.BaseDirectory, "db"));
}

if (!enforcePermissions)
{
    app.Logger.LogWarning(
        "Access:EnforcePermissions = false. Las politicas NO se exigen: cualquiera puede crear " +
        "plantillas, y una plantilla es ZPL con todos sus comandos. Solo para migrar.");
}

// --- Pipeline --------------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(DevCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// El health dice QUE ADAPTADOR esta activo y COMO VA LA COLA. Las dos son la primera pregunta
// cuando alguien reporta que "imprimio y no salio nada": muchas veces la respuesta es que estaba
// en simulador, o que hay cuarenta trabajos esperando a una impresora apagada.
app.MapGet("/health", async (IPrinterGateway gateway, IPrintQueue queue, CancellationToken cancellationToken) =>
{
    var byStatus = await queue.CountByStatusAsync(cancellationToken);
    return Results.Ok(new
    {
        status = "ok",
        printerDriver = gateway.Name,
        permissionsEnforced = enforcePermissions,
        queue = new
        {
            pending = byStatus.GetValueOrDefault(Printing.Domain.Entities.PrintQueueStatus.Pending),
            dead = byStatus.GetValueOrDefault(Printing.Domain.Entities.PrintQueueStatus.Dead),
        },
        utcTimeStamp = DateTime.UtcNow,
    });
}).AllowAnonymous();

await app.RunAsync();
