using BoslaPlatform.Application;
using BoslaPlatform.Application.Features.Conversations.Mappings;
using BoslaPlatform.Application.Features.Conversations.Services;
using BoslaPlatform.Application.Features.Lookup.Services;
using BoslaPlatform.Application.Features.Specialists.Services;
using BoslaPlatform.Application.Features.VideoSessions.Interfaces;
using BoslaPlatform.Application.Features.VideoSessions.Services;
using BoslaPlatform.Application.Interfaces.Conversation;
using BoslaPlatform.Application.Interfaces.Lookup;
using BoslaPlatform.Application.Interfaces.Specialists;
using FluentValidation;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IVideoSessionService, VideoSessionService>();

        services.AddAutoMapper(cfg =>{}, typeof(ConversationMappingProfile).Assembly);
        //services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyReference>();

        services.AddScoped<ISpecialistService, SpecialistService>();
        services.AddScoped<ILookupService, LookupService>();

        // Admin service implementation is registered in Infrastructure DI

        return services;
    }
}
