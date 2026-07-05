using BoslaPlatform.Application.Features.Payments.Dtos;
using BoslaPlatform.Application.Features.Payments.Requests;
using BoslaPlatform.Application.Features.Specialists.DTOs;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Payments;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Settings;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Application.Features.Payments.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IAppDbContext _context;
        private readonly IUser _currentUser;
        private readonly IPaymentGateway _paymentGateway;
        private readonly StripeSettings _stripeSettings;


        public PaymentService(
            IAppDbContext context,
            IUser currentUser,
            IPaymentGateway paymentGateway,
            IOptions<StripeSettings> stripeSettings)
        {
            _context = context;
            _currentUser = currentUser;
            _paymentGateway = paymentGateway;
            _stripeSettings = stripeSettings.Value;
        }

        public async Task<Result<PaymentResponseDto>> InitiateAsync(InitiatePaymentRequest request, CancellationToken ct = default)
        {
            if (!_currentUser.IsAuthenticated || !_currentUser.Id.HasValue)
            {
                return Error.Unauthorized("Auth.Unauthorized", "User must be authenticated to initiate payment.");
            }

            var appointment = await _context.Appointments
                .Include(a => a.Specialist)
                .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct);

            if (appointment is null)
            {
                return Error.NotFound("Appointment.NotFound", "The requested appointment was not found.");
            }

            if (appointment.UserId != _currentUser.Id.Value)
            {
                return Error.Forbidden("Payment.Forbidden", "You are not authorized to pay for this appointment.");
            }

            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.AppointmentId == request.AppointmentId && p.Status != PaymentStatus.Failed, ct);

            if (existingPayment is not null)
            {
                if (existingPayment.Status == PaymentStatus.Completed)
                {
                    return Error.Conflict("Payment.AlreadyPaid", "This appointment has already been paid for.");
                }

                if (!string.IsNullOrEmpty(existingPayment.ExternalPaymentId))
                {
                    var clientSecret = await _paymentGateway.GetPaymentIntentClientSecretAsync(existingPayment.ExternalPaymentId);
                    return MapToDto(existingPayment, clientSecret);
                }

                try
                {
                    var (clientSecret, paymentIntentId) = await _paymentGateway.CreatePaymentIntentAsync(existingPayment.Amount, existingPayment.Currency);
                    existingPayment.AssignExternalId(paymentIntentId);
                    await _context.SaveChangesAsync(ct);
                    return MapToDto(existingPayment, clientSecret);
                }
                catch (Exception ex)
                {
                    return Error.Unexpected("Payment.GatewayError", $"An error occurred with the payment provider: {ex.Message}");
                }
            }

            var payment = Payment.Initiate(appointment.Id, appointment.SessionPrice, request.Currency);

            try
            {
                var (clientSecret, paymentIntentId) = await _paymentGateway.CreatePaymentIntentAsync(payment.Amount, payment.Currency);

                payment.AssignExternalId(paymentIntentId);

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync(ct);

                return MapToDto(payment, clientSecret);
            }
            catch (Exception ex)
            {
                return Error.Unexpected("Payment.GatewayError", $"An error occurred with the payment provider: {ex.Message}");
            }
        }

        public async Task<Result<PaymentResponseDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var payment = await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (payment is null)
            {
                return Error.NotFound("Payment.NotFound", "The requested payment record was not found.");
            }

            return MapToDto(payment, null);
        }

        public async Task<Result<PaymentResponseDto>> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
        {
            var payment = await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.AppointmentId == appointmentId, ct);

            if (payment is null)
            {
                return Error.NotFound("Payment.NotFound", "No payment record found for this appointment.");
            }

            return MapToDto(payment, null);
        }
        
        public async Task<Result<IReadOnlyList<PaymentResponseDto>>> GetMyPaymentsAsync(CancellationToken ct = default)
        {
            if (!_currentUser.IsAuthenticated || !_currentUser.Id.HasValue)
            {
                return Error.Unauthorized("Auth.Unauthorized", "User must be authenticated.");
            }

            // A user's payments are those tied to their appointments
            var payments = await _context.Payments
                .Include(p => p.Appointment)
                .Where(p => p.Appointment.UserId == _currentUser.Id.Value && p.Status == PaymentStatus.Completed)
                .OrderByDescending(p => p.PaidAt)
                .AsNoTracking()
                .ToListAsync(ct);

            var dtos = payments.Select(p => MapToDto(p, null)).ToList();
            return dtos;
        }

        
        private PaymentResponseDto MapToDto(Payment payment, string? clientSecret)
        {
            return new PaymentResponseDto
            {
                Id = payment.Id,
                AppointmentId = payment.AppointmentId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Status = payment.Status,
                PaymentMethod = payment.PaymentMethod,
                ExternalPaymentId = payment.ExternalPaymentId,
                PaidAt = payment.PaidAt,
                PlatformFeeAmount = payment.PlatformFeeAmount,
                SpecialistAmount = payment.SpecialistAmount,
                TaxAmount = payment.TaxAmount,
                ClientSecret = clientSecret,
                SuccessUrl = _stripeSettings.SuccessUrl.Replace("{CHECKOUT_SESSION_ID}", payment.ExternalPaymentId),
                CancelUrl = _stripeSettings.CancelUrl
            };
        }
    }
}
