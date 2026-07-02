using BoslaPlatform.Infrastructure.Realtime;
using BoslaPlatform.API.Common.Filters;
using BoslaPlatform.Infrastructure.Data;
using BoslaPlatform.Infrastructure.DependencyInjection;

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

// Gemini AI is now the mandatory provider
builder.Services.AddGeminiAI();

// Rate limiting (disabled for now) — policy code previously added removed per request
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(7275, listen =>
    {
        listen.UseHttps("Certificates/bosla.pfx", "changeit");
    });

    options.ListenAnyIP(5250);
});
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

// Health check endpoint for monitoring infrastructure
app.MapHealthChecks("/health");

app.MapHub<BoslaPlatform.Infrastructure.RealTime.NotificationHub>("/hubs/notifications");
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<VideoHub>("/hubs/video");

app.Run();
