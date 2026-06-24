using BoslaPlatform.API.Common.Filters;
using BoslaPlatform.Infrastructure.Data;

// Add services to the container.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationFilter>();
    options.Conventions.Add(new BoslaPlatform.API.OpenApi.AddDefaultResponseConvention());
});

builder.Services.AddOpenApi();

builder.Services.AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation();

// Rate limiting (disabled for now) — policy code previously added removed per request

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
app.MapHub<BoslaPlatform.Infrastructure.RealTime.NotificationHub>("/hubs/notifications");

app.Run();
