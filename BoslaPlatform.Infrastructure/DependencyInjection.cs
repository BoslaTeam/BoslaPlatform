using System.Text;
using System.Text.Json;
using Polly;
using BoslaPlatform.Application;
using BoslaPlatform.Application.Common.Interfaces;
using BoslaPlatform.Application.Features.Admin.Services;
using BoslaPlatform.Application.Features.Appointments.Services;
using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Application.Features.Portfolio.Services;
using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Communication;
using BoslaPlatform.Application.Interfaces;
using BoslaPlatform.Application.Features.Admin.Repositories;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Specialists;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Application.Features.Specialists.Services;
using BoslaPlatform.Application.Features.VideoSessions.Interfaces;
using BoslaPlatform.Application.Features.VideoSessions.Services;
using BoslaPlatform.Application.Features.Favorites.Services;
using BoslaPlatform.Application.Features.Withdrawals.Services;
using BoslaPlatform.Application.Services;
using BoslaPlatform.Application.Settings;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Infrastructure.AI.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using BoslaPlatform.Infrastructure.Agora;
using BoslaPlatform.Infrastructure.Agora.Interfaces;
using BoslaPlatform.Infrastructure.Agora.Services;
using BoslaPlatform.Infrastructure.BackgroundJobs;
//using BoslaPlatform.Infrastructure.AI.OpenAi;
using BoslaPlatform.Infrastructure.Communication;
using BoslaPlatform.Infrastructure.Data;
using BoslaPlatform.Infrastructure.Data.Outbox;
using BoslaPlatform.Infrastructure.Favorites;
using BoslaPlatform.Infrastructure.Data.Interceptors;
using BoslaPlatform.Infrastructure.Identity;
using BoslaPlatform.Infrastructure.Realtime;
using BoslaPlatform.Infrastructure.RateLimiting;
using BoslaPlatform.Infrastructure.Services;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using BoslaPlatform.Infrastructure.Recording.Providers.Agora.Authentication;
using BoslaPlatform.Infrastructure.Recording.Providers.Agora.Services;
using BoslaPlatform.Infrastructure.Recording.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
            services.AddSingleton<UserPresenceTracker>();
        
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        ArgumentNullException.ThrowIfNull(connectionString);

        services.Configure<StripeSettings>(configuration.GetSection("StripeSettings"));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyReference).Assembly));

            // ── Interceptor execution order (required) ──────────────────────
            // SavingChanges (forward): Audit → Auditable → Outbox → DomainEvents
            //   Audit:        records audit trails (before any entity mutation)
            //   Auditable:    sets CreatedBy/LastModifiedBy
            //   Outbox:       serialises events → OutboxMessages (inside txn)
            //   DomainEvents: captures event snapshot for post-commit publish
            //
            // SavedChanges (reverse): DomainEvents → Outbox → Auditable → Audit
            //   DomainEvents: publishes via MediatR → THEN clears from entities
            //   Outbox:       nothing to do (messages already added in SavingChanges)
            //
            // WHY this order?
            //   Outbox must run BEFORE commit so outbox records are in the same
            //   transaction as domain data (atomicity). DomainEvents must run
            //   AFTER commit so local handlers see a consistent committed state.
            services.AddScoped<ISaveChangesInterceptor, AuditLogInterceptor>();
            services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
            services.AddScoped<ISaveChangesInterceptor, OutboxSaveChangesInterceptor>();
            services.AddScoped<ISaveChangesInterceptor, DomainEventsInterceptor>();
            services.AddScoped<ApplicationDbContextInitialiser>();

            // ── Outbox dispatcher ───────────────────────────────────────────
            services.Configure<OutboxDispatcherOptions>(
                configuration.GetSection(OutboxDispatcherOptions.SectionName));
            services.Configure<OutboxRetryOptions>(
                configuration.GetSection(OutboxRetryOptions.SectionName));
            services.AddSingleton<IEventTypeResolver, CachedEventTypeResolver>();
            services.AddScoped<IOutboxMessagePublisher, NoOpOutboxMessagePublisher>();
            services.AddHostedService<OutboxDispatcherService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
            services.AddScoped<IChatNotifier, SignalRChatNotifier>();
            services.AddScoped<IVideoNotifier, SignalRVideoNotifier>();
            services.AddScoped<IRecordingProvider, AgoraRecordingProvider>();
            services.AddScoped<ISTTProvider, NoOpSTTProvider>();
            services.AddScoped<IAgoraTokenService, AgoraTokenService>();

            services.AddHostedService<VideoSessionExpirationService>();

            services.Configure<VideoSessionExpirationOptions>(
                configuration.GetSection("VideoSessionExpiration"));

            services.AddRateLimitingPolicies(configuration);

            // Agora Cloud Recording — Typed HttpClient with authentication handler and retry policy
            var agoraSettings = configuration.GetSection(AgoraSettings.SectionName);
            var timeoutSeconds = agoraSettings.GetValue<int>(nameof(AgoraSettings.TimeoutSeconds));
            var retryCount = agoraSettings.GetValue<int>(nameof(AgoraSettings.RetryCount));

            services.AddTransient<AgoraAuthenticationHandler>();
            services.AddHttpClient<AgoraCloudRecordingApiClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds > 0 ? timeoutSeconds : 30);
            })
            .AddHttpMessageHandler<AgoraAuthenticationHandler>()
            .AddTransientHttpErrorPolicy(builder =>
                builder.WaitAndRetryAsync(
                    retryCount > 0 ? retryCount : 2,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
            .AddPolicyHandler(Policy<HttpResponseMessage>
                .HandleResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    retryCount > 0 ? retryCount : 2,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            // Agora Cloud Recording — health check (validates configuration without calling Agora APIs)
            services.AddHealthChecks()
                .AddCheck<AgoraRecordingHealthCheck>("agora-recording");

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
            options.UseSqlServer(connectionString, sqlOptions => sqlOptions.CommandTimeout(60));
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddScoped<IDashboardRepository>(provider =>
            new DapperDashboardRepository(connectionString));


        services.AddScoped<IAdminService, AdminService>();

        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddHostedService<AutoCancelUnpaidAppointmentsService>();
        services.AddScoped<INotificationSender, SignalRNotificationSender>();

        // TODO: Uncomment once tested
        // services.AddHostedService<ReminderBackgroundService>();
        services.AddScoped<IEmailService, EmailService>();
        // SignalR: use camelCase JSON so Angular handlers receive the expected
        // property names (userId, isOnline, lastSeen) instead of PascalCase.
        services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase;
            });

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

        // Gemini is the only AI provider (mandatory)
        services.Configure<GeminiSettings>(configuration.GetSection("GeminiSettings"));
        services.AddHttpClient("gemini").ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(30));

        services.AddHttpClient<BoslaPlatform.Infrastructure.AI.Gemini.GeminiEmbeddingService>();
        services.AddScoped<IEmbeddingService, BoslaPlatform.Infrastructure.AI.Gemini.GeminiEmbeddingService>();

        services.AddHttpClient<BoslaPlatform.Infrastructure.AI.Gemini.GeminiChatService>();
        services.AddScoped<IChatService, BoslaPlatform.Infrastructure.AI.Gemini.GeminiChatService>();
        services.AddScoped<IChatBotService, BoslaPlatform.Infrastructure.AI.ChatBotService>();

        // Register Qdrant settings early
        services.Configure<QdrantSettings>(configuration.GetSection("QdrantSettings"));

        // Register Semantic Kernel v1.77 with Gemini plugins
        services.AddSemanticKernelForGemini();

        // Register the official Semantic Kernel Qdrant connector (vector store)
        // This makes the Microsoft.SemanticKernel.Connectors.Qdrant vector store available via DI.
        var qdrantBaseUrl = configuration.GetSection("QdrantSettings")["BaseUrl"] ?? configuration.GetSection("QdrantSettings")["Url"];
        if (!string.IsNullOrEmpty(qdrantBaseUrl))
        {
            // AddQdrantVectorStore is an extension from Microsoft.SemanticKernel.Connectors.Qdrant
            services.AddQdrantVectorStore(qdrantBaseUrl);
            }

            // Register Qdrant HTTP client used by the local QdrantVectorStore implementation
            services.AddHttpClient<BoslaPlatform.Infrastructure.AI.Qdrant.QdrantClient>().ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(30));

            // Register application IVectorStore to the local QdrantVectorStore implementation
            // Keep the Semantic Kernel QdrantVectorStore registered by AddQdrantVectorStore for SK components
            services.AddScoped<IVectorStore, BoslaPlatform.Infrastructure.AI.Qdrant.QdrantVectorStore>();

            services.AddScoped<IAiSearchService, BoslaPlatform.Infrastructure.AI.AiSearchService>();
            services.AddScoped<IAiRecommendationService, BoslaPlatform.Infrastructure.AI.AiRecommendationService>();
            services.AddScoped<BoslaPlatform.Application.Interfaces.AI.IEmbeddingAdminService, BoslaPlatform.Infrastructure.AI.EmbeddingAdminService>();

        // Tokenizer implementation used by AiSearchService
        services.AddSingleton<BoslaPlatform.Infrastructure.AI.Tokenizers.ITokenizer, BoslaPlatform.Infrastructure.AI.Tokenizers.SimpleTokenizer>();

        // Summary service
        services.AddScoped<BoslaPlatform.Application.Interfaces.AI.ISummaryService, BoslaPlatform.Infrastructure.AI.SummaryService>();

        // Specialist AI services
        services.AddScoped<BoslaPlatform.Application.Interfaces.AI.ISpecialistAiService, BoslaPlatform.Infrastructure.AI.SpecialistAiService>();

        // Withdrawals / Payouts
        services.AddScoped<BoslaPlatform.Application.Interfaces.IWithdrawalService, BoslaPlatform.Application.Features.Withdrawals.Services.WithdrawalService>();

        // Wallet
        services.AddScoped<BoslaPlatform.Application.Interfaces.Wallets.IWalletService, BoslaPlatform.Application.Features.Wallets.Services.WalletService>();

        // Favorites
        services.AddScoped<IFavoriteService, FavoriteService>();

        // Portfolio
        services.AddScoped<IPortfolioService, BoslaPlatform.Infrastructure.Portfolio.PortfolioService>();

            services.AddAuthorization();


            return services;
        }
    }

