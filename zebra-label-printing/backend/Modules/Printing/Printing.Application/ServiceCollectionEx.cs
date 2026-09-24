using Common.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Printing.Application.Services;
using Printing.Application.UseCases.Jobs;
using Printing.Application.UseCases.Queue;
using Printing.Application.UseCases.Printers;
using Printing.Application.UseCases.Templates;

namespace Printing.Application;

/// <summary>
/// Todos los handlers se registran A MANO. No hay escaneo de ensamblados a proposito: un registro
/// que falta revienta al llamar el endpoint con un mensaje que dice exactamente que interfaz
/// falto, y no en un arranque magico que nadie sabe leer.
/// </summary>
public static class ServiceCollectionEx
{
    public static IServiceCollection AddPrintingApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        // El acto de imprimir en un solo sitio: lo usan los dos endpoints de impresion y el
        // reintento de la cola.
        services.AddScoped<PrintDispatchService>();
        services.AddScoped<PrintQueueWorker>();

        services.AddScoped<IRequestHandler<ListPrintersRequest, ListPrintersResponse>, ListPrintersHandler>();
        services.AddScoped<IRequestHandler<GetPrinterStatusRequest, GetPrinterStatusResponse>, GetPrinterStatusHandler>();
        services.AddScoped<IRequestHandler<SendRawZplRequest, SendRawZplResponse>, SendRawZplHandler>();

        services.AddScoped<IRequestHandler<ListTemplatesRequest, ListTemplatesResponse>, ListTemplatesHandler>();
        services.AddScoped<IRequestHandler<GetTemplateRequest, GetTemplateResponse>, GetTemplateHandler>();
        services.AddScoped<IRequestHandler<SaveTemplateRequest, SaveTemplateResponse>, SaveTemplateHandler>();
        services.AddScoped<IRequestHandler<DeactivateTemplateRequest, DeactivateTemplateResponse>, DeactivateTemplateHandler>();
        services.AddScoped<IRequestHandler<PrintTemplateRequest, PrintTemplateResponse>, PrintTemplateHandler>();

        services.AddScoped<IRequestHandler<ListPrintJobsRequest, ListPrintJobsResponse>, ListPrintJobsHandler>();
        services.AddScoped<IRequestHandler<GetTemplateHistoryRequest, GetTemplateHistoryResponse>, GetTemplateHistoryHandler>();
        services.AddScoped<IRequestHandler<PreviewTemplateRequest, PreviewTemplateResponse>, PreviewTemplateHandler>();
        services.AddScoped<IRequestHandler<ListPrintQueueRequest, ListPrintQueueResponse>, ListPrintQueueHandler>();
        services.AddScoped<IRequestHandler<CancelPrintQueueItemRequest, CancelPrintQueueItemResponse>, CancelPrintQueueItemHandler>();

        return services;
    }
}
