using System.Text;
using BoslaPlatform.Application;
using BoslaPlatform.Application.Common.Interfaces;
using BoslaPlatform.Application.Features.Admin.Services;
using BoslaPlatform.Application.Features.Appointments.Services;
using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Communication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Application.Features.VideoSessions.Interfaces;
using BoslaPlatform.Application.Features.VideoSessions.Services;
using BoslaPlatform.Application.Services;
using BoslaPlatform.Application.Settings;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Infrastructure.Agora;
using BoslaPlatform.Infrastructure.Agora.Interfaces;
using BoslaPlatform.Infrastructure.Agora.Services;
using BoslaPlatform.Infrastructure.AI.OpenAi;
using BoslaPlatform.Infrastructure.Communication;
using BoslaPlatform.Infrastructure.Data;
using BoslaPlatform.Infrastructure.Data.Interceptors;
using BoslaPlatform.Infrastructure.Identity;
using BoslaPlatform.Infrastructure.Realtime;
using BoslaPlatform.Infrastructure.Services;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        ArgumentNullException.ThrowIfNull(connectionString);

        services.Configure<StripeSettings>(configuration.GetSection("StripeSettings"));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyReference).Assembly));

            services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
            services.AddScoped<ISaveChangesInterceptor, DomainEventsInterceptor>();
            services.AddScoped<ApplicationDbContextInitialiser>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IChatNotifier, SignalRChatNotifier>();
            services.AddScoped<IVideoNotifier, SignalRVideoNotifier>();
            services.AddScoped<IAgoraTokenService, AgoraTokenService>();

            // Agora Webhook — Phase 1
            // IAgoraWebhookSignatureVerifier: Infrastructure concern (HMAC-SHA256 + replay window).
            //   Registered as Singleton because it is stateless and only reads from IOptions<AgoraSettings>.
            services.AddSingleton<IAgoraWebhookSignatureVerifier, AgoraWebhookSignatureVerifier>();

            // IVideoSessionWebhookService: Application concern (business orchestration).
            //   Registered as Scoped because it depends on the scoped IAppDbContext.
            services.AddScoped<IVideoSessionWebhookService, VideoSessionWebhookService>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddScoped<IAdminService, AdminService>();

        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<INotificationSender, SignalRNotificationSender>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddSignalR();

        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.Configure<GoogleSettings>(configuration.GetSection("GoogleSettings"));
        services.AddOptions<AgoraSettings>().Bind(configuration.GetSection(AgoraSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

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
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();
        services.AddHttpContextAccessor();

        services.Configure<OpenAISettings>(configuration.GetSection("OpenAISettings"));
        services.AddHttpClient("openai").ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10));
        services.AddScoped<IChatService, OpenAiChatService>();
        
        services.AddHttpClient<OpenAiEmbeddingService>();
        services.AddScoped<IEmbeddingService,OpenAiEmbeddingService>();
        
        services.AddHttpClient<OpenAiChatService>();
        services.AddSingleton<BoslaPlatform.Infrastructure.AI.Tokenizers.ITokenizer, BoslaPlatform.Infrastructure.AI.Tokenizers.SimpleTokenizer>();
        
        services.Configure<QdrantSettings>(configuration.GetSection("QdrantSettings"));
        services.AddHttpClient<BoslaPlatform.Infrastructure.AI.Qdrant.QdrantClient>().ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10));

        services.AddScoped<IVectorStore, BoslaPlatform.Infrastructure.AI.Qdrant.QdrantVectorStore>();
        services.AddScoped<IAiSearchService, BoslaPlatform.Infrastructure.AI.AiSearchService>();
                        
            services.AddAuthorization();


            return services;
        }
    }
