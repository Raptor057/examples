using Common.Messaging;
using Common.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Printing.Application.UseCases.Jobs;
using Printing.Application.UseCases.Queue;
using Printing.Application.UseCases.Printers;
using Printing.Application.UseCases.Templates;
using Printing.Presentation.Controllers;
using Printing.Presentation.Presenters;

namespace Printing.Presentation;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddPrintingPresentation(this IServiceCollection services)
    {
        // Un view model POR PETICION y por controller. Scoped, no singleton: con singleton dos
        // peticiones simultaneas se pisarian la respuesta la una a la otra.
        services.AddScoped(typeof(ResultViewModel<>));

        // Cada presenter va sobre el tipo BASE de la respuesta. Si falta uno, su endpoint
        // responde el envelope vacio y NADA en la compilacion lo delata.
        services.AddScoped<INotificationHandler<ListPrintersResponse>, ListPrintersPresenter>();
        services.AddScoped<INotificationHandler<GetPrinterStatusResponse>, GetPrinterStatusPresenter>();
        services.AddScoped<INotificationHandler<SendRawZplResponse>, SendRawZplPresenter>();
        services.AddScoped<INotificationHandler<PrintTemplateResponse>, PrintTemplatePresenter>();
        services.AddScoped<INotificationHandler<ListPrintJobsResponse>, ListPrintJobsPresenter>();
        services.AddScoped<INotificationHandler<GetTemplateHistoryResponse>, GetTemplateHistoryPresenter>();
        services.AddScoped<INotificationHandler<PreviewTemplateResponse>, PreviewTemplatePresenter>();
        services.AddScoped<INotificationHandler<ListPrintQueueResponse>, ListPrintQueuePresenter>();
        services.AddScoped<INotificationHandler<CancelPrintQueueItemResponse>, CancelPrintQueueItemPresenter>();
        services.AddScoped<INotificationHandler<ListTemplatesResponse>, ListTemplatesPresenter>();
        services.AddScoped<INotificationHandler<GetTemplateResponse>, GetTemplatePresenter>();
        services.AddScoped<INotificationHandler<SaveTemplateResponse>, SaveTemplatePresenter>();
        services.AddScoped<INotificationHandler<DeactivateTemplateResponse>, DeactivateTemplatePresenter>();

        return services;
    }
}
