using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Events.Videos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.EventHandlers.Videos
{
    public sealed class VideoSessionEndedEventHandler
    : INotificationHandler<VideoSessionEndedEvent>
    {
        private readonly IAppDbContext _context;

        public VideoSessionEndedEventHandler(
            IAppDbContext context)
        {
            _context = context;
        }

        public async Task Handle(VideoSessionEndedEvent notification,CancellationToken cancellationToken)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(
                    x => x.Id == notification.AppointmentId,
                    cancellationToken);

            if (appointment is null)
            {
                return;
            }

            appointment.Complete(
                appointment.SpecialistId);

            await _context.SaveChangesAsync(
                cancellationToken);
        }
    }
}
