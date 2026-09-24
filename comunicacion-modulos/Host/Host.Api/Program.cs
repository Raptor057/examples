using System.Diagnostics;
using System.Reflection;
using Common.Messaging;
using Common.Observability;
using Geo.Application;
using Geo.Infrastructure;
using Geo.Presentation;
using Identity.Infrastructure;
using Inventario.Application;
using Inventario.Infrastructure;
using Inventario.Presentation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Ventas.Application;
using Ventas.Infrastructure;
using Ventas.Presentation;

var startedAtUtc = DateTime.UtcNow;

var builder = WebApplication.CreateBuilder(args);

// ---- Observabilidad (Common): habilita metricas Prometheus en /metrics. ----
builder.Services.AddObservability(builder.Configuration, "ComunicacionModulos.Api");

// ---- Mediator: escanea los assemblies de Application con handlers. ----
builder.Services.AddMediator(
    typeof(Inventario.Application.ServiceCollectionEx).Assembly,
    typeof(Ventas.Application.ServiceCollectionEx).Assembly,
    typeof(Geo.Application.ServiceCollectionEx).Assembly);

// ---- Modulos GLOBALES ----
builder.Services.AddIdentityInfrastructureServices();
builder.Services.AddGeoApplicationServices();
builder.Services.AddGeoInfrastructureServices();
builder.Services.AddGeoPresentationServices();

// ---- Modulos de NEGOCIO ----
builder.Services.AddInventarioApplicationServices();
builder.Services.AddInventarioInfrastructureServices();
builder.Services.AddInventarioPresentationServices();

builder.Services.AddVentasApplicationServices();
builder.Services.AddVentasInfrastructureServices();
builder.Services.AddVentasPresentationServices();

// ---- Health checks ----
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API arriba"));

// ---- Controllers (uno por modulo, en su assembly de Presentation) ----
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Geo.Presentation.ServiceCollectionEx).Assembly)
    .AddApplicationPart(typeof(Inventario.Presentation.ServiceCollectionEx).Assembly)
    .AddApplicationPart(typeof(Ventas.Presentation.ServiceCollectionEx).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Comunicacion entre modulos (globales + negocio)",
        Version = "v1"
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
    c.DocumentTitle = "Comunicacion entre modulos - Swagger";
});

// ---- /api/health (endpoint normal para que Swagger lo documente) ----
app.MapGet("/api/health", async (HealthCheckService health) =>
{
    var report = await health.CheckHealthAsync();
    var payload = new
    {
        status = report.Status.ToString(),
        totalMs = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new { nombre = e.Key, estado = e.Value.Status.ToString(), e.Value.Description })
    };
    return Results.Json(payload, statusCode: report.Status == HealthStatus.Healthy ? 200 : 503);
})
.WithTags("Diagnostico")
.WithSummary("Estado de salud de la API.");

// ---- /api/info: entorno, uptime y version ----
app.MapGet("/api/info", (IWebHostEnvironment env) =>
{
    var version = Environment.GetEnvironmentVariable("APP_VERSION")
        ?? Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0";

    var uptime = DateTime.UtcNow - startedAtUtc;

    return Results.Ok(new
    {
        servicio = "Comunicacion entre modulos",
        entorno = env.EnvironmentName,
        version,
        uptime = new
        {
            inicioUtc = startedAtUtc,
            segundos = (long)uptime.TotalSeconds,
            legible = $"{uptime.Days}d {uptime.Hours:D2}h {uptime.Minutes:D2}m {uptime.Seconds:D2}s"
        },
        maquina = Environment.MachineName,
        framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        ahoraUtc = DateTime.UtcNow
    });
})
.WithTags("Diagnostico")
.WithSummary("Entorno, version y uptime del servicio.");

// ---- /metrics (Prometheus, via OpenTelemetry). No va en Swagger: es texto Prometheus para scraping. ----
app.MapPrometheusScrapingEndpoint();

app.MapControllers();

// La raiz redirige a Swagger; se excluye de la documentacion .
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// ---- Al iniciar, abrir el navegador en /swagger (solo dev; se omite si NO_OPEN_BROWSER=1). ----
if (app.Environment.IsDevelopment() && Environment.GetEnvironmentVariable("NO_OPEN_BROWSER") is null)
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var baseUrl = app.Urls.FirstOrDefault() ?? "http://localhost:5200";
        AbrirNavegador($"{baseUrl}/swagger");
    });
}

app.Run();

static void AbrirNavegador(string url)
{
    try
    {
        if (OperatingSystem.IsMacOS())
            Process.Start("open", url);
        else if (OperatingSystem.IsWindows())
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        else if (OperatingSystem.IsLinux())
            Process.Start("xdg-open", url);
    }
    catch
    {
        // Si no se puede abrir el navegador (p.ej. headless), se ignora.
    }
}
