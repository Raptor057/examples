using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Printing.Domain.Abstractions;
using Printing.Infrastructure.Persistence;
using Printing.Infrastructure.Printers;

namespace Printing.Infrastructure;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddPrintingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PrintingOptions>(configuration.GetSection(PrintingOptions.SectionName));
        services.Configure<PreviewOptions>(configuration.GetSection(PreviewOptions.SectionName));

        // HttpClient con nombre y con TIEMPO DE ESPERA. Sin el, el cliente por omision espera
        // 100 segundos, y una vista previa que cuelga la peticion dos minutos se siente como
        // una aplicacion rota.
        var previewTimeout = configuration.GetValue<int?>($"{PreviewOptions.SectionName}:TimeoutSeconds") ?? 10;
        services.AddHttpClient<ILabelPreviewRenderer, LabelaryPreviewRenderer>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(previewTimeout);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png"));
        });

        services.AddScoped<ILabelTemplateRepository, LabelTemplateRepository>();
        services.AddScoped<IPrintJobLog, PrintJobLog>();
        services.AddScoped<IPrintQueue, PrintQueueRepository>();

        // Los DOS adaptadores se registran como tipos concretos, y el puerto se resuelve segun la
        // configuracion. Asi cambiar de simulador a impresoras reales es una linea de appsettings
        // y un reinicio, no una recompilacion. Ver ADR-0004.
        services.AddScoped<SimulatorPrinterGateway>();
        services.AddScoped<ZebraLinkOsPrinterGateway>();

        services.AddScoped<IPrinterGateway>(provider =>
        {
            var driver = configuration[$"{PrintingOptions.SectionName}:Driver"]?.Trim().ToLowerInvariant();
            return driver switch
            {
                "zebra" => provider.GetRequiredService<ZebraLinkOsPrinterGateway>(),
                "simulator" or null or "" => provider.GetRequiredService<SimulatorPrinterGateway>(),

                // Un valor mal escrito NO cae al simulador en silencio: alguien creeria que esta
                // imprimiendo de verdad y estaria llenando una carpeta de archivos.
                _ => throw new InvalidOperationException(
                    $"Printing:Driver = '{driver}' no existe. Usa 'zebra' o 'simulator'."),
            };
        });

        return services;
    }
}
