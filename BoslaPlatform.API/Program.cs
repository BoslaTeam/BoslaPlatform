using BoslaPlatform.Infrastructure.Realtime;
using BoslaPlatform.API.Common.Filters;
using BoslaPlatform.Infrastructure.Data;
using BoslaPlatform.Infrastructure.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
if (!Directory.Exists(webRootPath))
{
    Directory.CreateDirectory(webRootPath);
}
builder.Environment.WebRootPath = webRootPath;

builder.Services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationFilter>();
    options.Conventions.Add(new BoslaPlatform.API.OpenApi.AddDefaultResponseConvention());
});

builder.Services.AddOpenApi();

builder.Services.AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation();

// ── OpenTelemetry: trace the recording pipeline and expose Prometheus metrics ──
// Tracing registers the "Bosla.Recording" ActivitySource so each pipeline stage
// becomes a span (correlated by the recording.correlation_id tag). Metrics
// register the "Bosla.Recording" Meter and expose it at /metrics for Prometheus.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(BoslaPlatform.Infrastructure.Observability.RecordingObservabilityNames.ActivitySource)
        .AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddMeter(BoslaPlatform.Infrastructure.Observability.RecordingObservabilityNames.Meter)
        .AddPrometheusExporter());

// Gemini AI is now the mandatory provider
builder.Services.AddGeminiAI();

// Rate limiting (disabled for now) — policy code previously added removed per request
//builder.WebHost.ConfigureKestrel(options =>
//{
//    options.ListenAnyIP(7275, listen =>
//    {
//        listen.UseHttps("Certificates/bosla.pfx", "changeit");
//    });

//    options.ListenAnyIP(5250);
//});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Show developer exception page so OpenAPI generation errors are visible during development
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "BoslaPlatform API V1");

        options.EnableDeepLinking();
        options.DisplayRequestDuration();
        options.EnableFilter();
    });
    await app.InitialiseDatabaseAsync();
}

app.UseCoreMiddlewares(builder.Configuration);
app.MapControllers();

// Health check endpoint for monitoring infrastructure.
// Writes each check's description and data, because a bare "Unhealthy" forces
// an operator back into the code to find out which dependency broke.
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds,
                tags = e.Value.Tags,
                data = e.Value.Data,
                error = e.Value.Exception?.Message
            })
        });
    }
});

// Prometheus scrape endpoint for the "Bosla.Recording" meter (and ASP.NET Core metrics).
app.MapPrometheusScrapingEndpoint(); // GET /metrics

app.MapHub<BoslaPlatform.Infrastructure.RealTime.NotificationHub>("/hubs/notifications");
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<VideoHub>("/hubs/video");

app.Run();
