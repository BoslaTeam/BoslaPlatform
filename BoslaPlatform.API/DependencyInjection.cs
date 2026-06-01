using Asp.Versioning;
using BoslaPlatform.API.Middleware;
using BoslaPlatform.API.Services;
using BoslaPlatform.Application.Interfaces;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        // Register presentation services here
        services.AddIdentityInfrastructure()
            .AddCustomProblemDetails()
            .AddEndpointsApiExplorer()
            .AddSwaggerGen()
            .AddCustomApiVersioning();
        return services;
    }

    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUser, CurrentUser>();
        services.AddHttpContextAccessor();
        return services;
    }
    public static IServiceCollection AddCustomProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = (context) =>
        {
            context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            context.ProblemDetails.Extensions.Add("requestId", context.HttpContext.TraceIdentifier);
        });

        return services;
    }
    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }
    public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddMvc();


        return services;
    }

    public static IApplicationBuilder UseCoreMiddlewares(this IApplicationBuilder app, IConfiguration configuration)
    {
        app.UseMiddleware<PerformanceMiddleware>();
        app.UseMiddleware<RequestLogContextMiddleware>();
        app.UseExceptionHandler();

        app.UseStatusCodePages();

        app.UseHttpsRedirection();

        app.UseAuthentication();

        app.UseAuthorization();

        return app;
    }
}
