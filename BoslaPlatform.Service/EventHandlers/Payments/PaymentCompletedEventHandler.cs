using System.Threading;
using System.Threading.Tasks;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Events.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Application.EventHandlers.Payments
{
    public sealed class PaymentCompletedEventHandler
        : INotificationHandler<PaymentCompletedEvent>
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<PaymentCompletedEventHandler> _logger;

        public PaymentCompletedEventHandler(
            IAppDbContext context,
            ILogger<PaymentCompletedEventHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Handle(PaymentCompletedEvent notification, CancellationToken ct)
        {
            var payment = await _context.Payments
                .AsNoTracking()
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(p => p.Id == notification.PaymentId, ct);

            if (payment is null)
            {
                _logger.LogWarning("PaymentCompletedEvent: Payment {PaymentId} not found", notification.PaymentId);
                return;
            }

            _logger.LogInformation(
                "Payment {PaymentId} for appointment {AppointmentId} completed. Amount: {Amount}, ExternalId: {ExternalId}",
                notification.PaymentId,
                notification.AppointmentId,
                notification.Amount,
                notification.ExternalPaymentId);
        }
    }
}
