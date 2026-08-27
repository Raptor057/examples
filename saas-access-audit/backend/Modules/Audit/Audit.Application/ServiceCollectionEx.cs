using Audit.Application.UseCases.GetAccessDenials;
using Audit.Application.UseCases.GetAccessDenials.Responses;
using Audit.Application.UseCases.GetActionAudit;
using Audit.Application.UseCases.GetActionAudit.Responses;
using Common.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Application;

public static class ServiceCollectionEx
{
    public static IServiceCollection AddAuditApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<GetActionAuditRequest, GetActionAuditResponse>, GetActionAuditHandler>();
        services.AddScoped<IRequestHandler<GetAccessDenialsRequest, GetAccessDenialsResponse>, GetAccessDenialsHandler>();
        return services;
    }
}
