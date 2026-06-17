using BoslaPlatform.Domain.Events.Specialists;
using MediatR;

namespace BoslaPlatform.Application.Features.Specialists.EventHandlers
{
    public sealed class SpecialistOnboardedEventHandler : INotificationHandler<SpecialistOnboardedEvent>
    {
        public Task Handle(SpecialistOnboardedEvent notification,  CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
