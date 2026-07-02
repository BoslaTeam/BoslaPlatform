using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Domain.Events.Apoointments;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Application.Features.Appointments.EventHandlers
{
    public sealed class AppointmentCompletedHandler : INotificationHandler<AppointmentCompletedEvent>
    {
        private readonly ISummaryService _summaryService;
        private readonly ILogger<AppointmentCompletedHandler> _logger;

        public AppointmentCompletedHandler(ISummaryService summaryService, ILogger<AppointmentCompletedHandler> logger)
        {
            _summaryService = summaryService;
            _logger = logger;
        }

        public async Task Handle(AppointmentCompletedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Auto-generating summary for appointment {Id}", notification.AppointmentId);

            var result = await _summaryService.RegenerateAsync(notification.AppointmentId, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Summary generated successfully for appointment {Id}", notification.AppointmentId);
            }
            else
            {
                _logger.LogWarning("Failed to auto-generate summary for appointment {Id}: {Error}",
                    notification.AppointmentId, result.Errors.Count > 0 ? result.Errors[0].Description : "Unknown error");
            }
        }
    }
}
