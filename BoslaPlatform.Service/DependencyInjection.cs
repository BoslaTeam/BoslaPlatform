
using BoslaPlatform.Application.Features.Conversations.Mappings;
using BoslaPlatform.Application.Features.Conversations.Services;
using BoslaPlatform.Application.Interfaces.Conversation;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IMessageService, MessageService>();

        services.AddAutoMapper(cfg =>{}, typeof(ConversationMappingProfile).Assembly);
        return services;
    }
}
