using System.Threading;
using System.Threading.Tasks;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Events;
using BoslaPlatform.Domain.Events.Apoointments;
using BoslaPlatform.Domain.Models.Communication;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.EventHandlers.Appointments
{
    public sealed class AppointmentCreatedEventHandler
        : INotificationHandler<AppointmentScheduledEvent>
    {
        private readonly IAppDbContext _context;

        public AppointmentCreatedEventHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task Handle(
            AppointmentScheduledEvent notification,
            CancellationToken ct)
        {


            var existingConversation = await _context.Conversations
                .AnyAsync(c => c.AppointmentId == notification.AppointmentId, ct);

            if (existingConversation)
            {
                return;
            }

            var conversationResult = Conversation.CreateForAppointment(
                notification.AppointmentId,
                notification.UserId,
                notification.SpecialistId
            );
 
            if (conversationResult.IsSuccess)
            {
                _context.Conversations.Add(conversationResult.Value);
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}