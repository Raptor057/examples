using Common.Messaging;
using Common.Results;
using Common.ViewModels;
using Printing.Application.Dtos;
using Printing.Application.UseCases.Jobs;
using Printing.Application.UseCases.Queue;
using Printing.Application.UseCases.Printers;
using Printing.Application.UseCases.Templates;
using Printing.Presentation.Controllers;

namespace Printing.Presentation.Presenters;

/*
 * LOS PRESENTERS. Es la pieza que mas se olvida de toda la arquitectura, y la unica que el
 * compilador no vigila.
 *
 * El controller no serializa nada: publica la respuesta del handler y devuelve el view model.
 * Quien llena ese view model es el presenter registrado para el tipo BASE de la respuesta. Si
 * falta, el endpoint contesta el envelope por omision:
 *
 *     { "data": null, "isSuccess": false, "message": "", "utcTimeStamp": "..." }
 *
 * y eso no es un error de negocio ni de SQL: son los valores iniciales del ResultViewModel porque
 * nadie lo toco. El proyecto compila, las consultas corren perfecto, y el fallo solo aparece al
 * llamar la API. Por eso todos los presenters viven en UN archivo: para que se vea de un vistazo
 * si falta el de un caso de uso. Ver ADR-0006.
 *
 * El registro esta en ServiceCollectionEx, y va sobre el tipo BASE de la respuesta -no sobre el
 * Success concreto-, porque Publish resuelve por el tipo estatico.
 */

public sealed class ListPrintersPresenter(ResultViewModel<PrintersController> viewModel)
    : INotificationHandler<ListPrintersResponse>
{
    public Task Handle(ListPrintersResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure) viewModel.Fail(failure.Message);
        else if (response is ISuccess<IReadOnlyList<PrinterDto>> ok) viewModel.Set(ok, data => data);
        return Task.CompletedTask;
    }
}

public sealed class GetPrinterStatusPresenter(ResultViewModel<PrintersController> viewModel)
    : INotificationHandler<GetPrinterStatusResponse>
{
    public Task Handle(GetPrinterStatusResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure) viewModel.Fail(failure.Message);
        else if (response is ISuccess<PrinterStatusDto> ok) viewModel.Set(ok, data => data);
        return Task.CompletedTask;
    }
}

public sealed class SendRawZplPresenter(ResultViewModel<PrintController> viewModel)
    : INotificationHandler<SendRawZplResponse>
{
    public Task Handle(SendRawZplResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure) viewModel.Fail(failure.Message);
        else if (response is ISuccess<TemplatePrintResultDto> ok) viewModel.Set(ok, data => data);
        return Task.CompletedTask;
    }
}

public sealed class GetTemplateHistoryPresenter(ResultViewModel<TemplatesController> viewModel)
    : INotificationHandler<GetTemplateHistoryResponse>
{
    public Task Handle(GetTemplateHistoryResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure) viewModel.Fail(failure.Message);
        else if (response is ISuccess<IReadOnlyList<TemplateVersionDto>> ok) viewModel.Set(ok, data => data);
        return Task.CompletedTask;
    }
}

/// <summary>
/// La vista previa exitosa devuelve una IMAGEN y no pasa por el envelope, asi que este presenter
/// solo tiene que atender el fallo. Aun asi existe: sin el, un fallo devolveria el sobre vacio.
/// </summary>
public sealed class PreviewTemplatePresenter(ResultViewModel<TemplatesController> viewModel)
    : INotificationHandler<PreviewTemplateResponse>
{
    public Task Handle(PreviewTemplateResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure) viewModel.Fail(failure.Message);
        else viewModel.Set();
        return Task.CompletedTask;
    }
}

public sealed class ListPrintQueuePresenter(ResultViewModel<PrintQueueController> viewModel)
    : INotificationHandler<ListPrintQueueResponse>
{
    public Task Handle(ListPrintQueueResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure) viewModel.Fail(failure.Message);
        else if (response is ISuccess<IReadOnlyList<PrintQueueItemDto>> ok) viewModel.Set(ok, data => data);
        return Task.CompletedTask;
    }
}

public sealed class CancelPrintQueueItemPresenter(ResultViewModel<PrintQueueController> viewModel)
    : INotificationHandler<CancelPrintQueueItemResponse>
{
    public Task Handle(CancelPrintQueueItemResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure) viewModel.Fail(failure.Message);
        else viewModel.Set("Trabajo cancelado.");
        return Task.CompletedTask;
    }
}

public sealed class PrintTemplatePresenter(ResultViewModel<PrintController> viewModel)
    : INotificationHandler<PrintTemplateResponse>
{
    public Task Handle(PrintTemplateResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure) viewModel.Fail(failure.Message);
        else if (response is ISuccess<TemplatePrintResultDto> ok) viewModel.Set(ok, data => data);
        return Task.CompletedTask;
    }
}

public sealed class ListPrintJobsPresenter(ResultViewModel<PrintController> viewModel)
    : INotificationHandler<ListPrintJobsResponse>
{
    public Task Handle(ListPrintJobsResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure) viewModel.Fail(failure.Message);
        else if (response is ISuccess<IReadOnlyList<PrintJobDto>> ok) viewModel.Set(ok, data => data);
        return Task.CompletedTask;
    }
}

public sealed class ListTemplatesPresenter(ResultViewModel<TemplatesController> viewModel)
    : INotificationHandler<ListTemplatesResponse>
{
    public Task Handle(ListTemplatesResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure) viewModel.Fail(failure.Message);
        else if (response is ISuccess<IReadOnlyList<TemplateDto>> ok) viewModel.Set(ok, data => data);
        return Task.CompletedTask;
    }
}

public sealed class GetTemplatePresenter(ResultViewModel<TemplatesController> viewModel)
    : INotificationHandler<GetTemplateResponse>
{
    public Task Handle(GetTemplateResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure) viewModel.Fail(failure.Message);
        else if (response is ISuccess<TemplateDto> ok) viewModel.Set(ok, data => data);
        return Task.CompletedTask;
    }
}

public sealed class SaveTemplatePresenter(ResultViewModel<TemplatesController> viewModel)
    : INotificationHandler<SaveTemplateResponse>
{
    public Task Handle(SaveTemplateResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure) viewModel.Fail(failure.Message);
        else if (response is ISuccess<TemplateDto> ok) viewModel.Set(ok, data => data);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Un exito SIN datos. Se ve distinto de los demas y por eso se deja explicito: llamar a Set()
/// sin argumentos es lo que pone isSuccess en true con data en null, que es la respuesta correcta
/// a una baja logica.
/// </summary>
public sealed class DeactivateTemplatePresenter(ResultViewModel<TemplatesController> viewModel)
    : INotificationHandler<DeactivateTemplateResponse>
{
    public Task Handle(DeactivateTemplateResponse response, CancellationToken cancellationToken)
    {
        if (response is IFailure failure) viewModel.Fail(failure.Message);
        else viewModel.Set("Plantilla dada de baja.");
        return Task.CompletedTask;
    }
}
