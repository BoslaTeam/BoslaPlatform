using BoslaPlatform.Application;
using BoslaPlatform.Application.Features.Appointments.Services;
using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Specialists;
using BoslaPlatform.Application.Services;
using BoslaPlatform.Application.Settings;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Infrastructure.Data;
using BoslaPlatform.Infrastructure.Data.Interceptors;
using BoslaPlatform.Infrastructure.Identity;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Net.Http;
using BoslaPlatform.Infrastructure.Communication;
using BoslaPlatform.Application.Interfaces.Communication;
using BoslaPlatform.Infrastructure.AI.OpenAi;
using BoslaPlatform.Application.Interfaces.AI;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        ArgumentNullException.ThrowIfNull(connectionString);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyReference).Assembly));

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DomainEventsInterceptor>();
        services.AddScoped<ApplicationDbContextInitialiser>();

        // Identity & Auth
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();

        // Admin service (implementation in Infrastructure)
        services.AddScoped<BoslaPlatform.Application.Features.Admin.Services.IAdminService, BoslaPlatform.Infrastructure.Services.AdminService>();

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAppointmentService, AppointmentService>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // Communication
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationSender, SignalRNotificationSender>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddSignalR();

        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredUniqueChars = 1;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
            };
        });

        services.AddAuthorization();
        services.AddHttpContextAccessor();

        // AI Services
        services.Configure<OpenAISettings>(configuration.GetSection("OpenAISettings"));
        services.AddHttpClient("openai").ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10));
        services.AddScoped<IChatService, OpenAiChatService>();
        services.AddHttpClient<BoslaPlatform.Infrastructure.AI.OpenAi.OpenAiEmbeddingService>();
        services.AddScoped<BoslaPlatform.Application.Interfaces.AI.IEmbeddingService, BoslaPlatform.Infrastructure.AI.OpenAi.OpenAiEmbeddingService>();
        services.AddHttpClient<BoslaPlatform.Infrastructure.AI.OpenAi.OpenAiChatService>();
        services.AddSingleton<BoslaPlatform.Infrastructure.AI.Tokenizers.ITokenizer, BoslaPlatform.Infrastructure.AI.Tokenizers.SimpleTokenizer>();
        services.Configure<QdrantSettings>(configuration.GetSection("QdrantSettings"));
        services.AddHttpClient<BoslaPlatform.Infrastructure.AI.Qdrant.QdrantClient>().ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10));

        // Use Qdrant-backed vector store for production; fallback to EF Core if Qdrant unavailable
        services.AddScoped<BoslaPlatform.Application.Interfaces.AI.IVectorStore, BoslaPlatform.Infrastructure.AI.Qdrant.QdrantVectorStore>();
        services.AddScoped<BoslaPlatform.Application.Interfaces.AI.IAiSearchService, BoslaPlatform.Infrastructure.AI.AiSearchService>();

        return services;
    }
}
