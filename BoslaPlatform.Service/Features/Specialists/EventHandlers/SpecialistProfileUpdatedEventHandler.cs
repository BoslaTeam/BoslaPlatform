using BoslaPlatform.Domain.Events.Specialists;
using MediatR;

namespace BoslaPlatform.Application.Features.Specialists.EventHandlers
{
    public sealed class SpecialistProfileUpdatedEventHandler : INotificationHandler<SpecialistProfileUpdatedEvent>
    {
        public Task Handle(SpecialistProfileUpdatedEvent notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
