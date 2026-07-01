using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Payments.Dtos;
using BoslaPlatform.Application.Features.Payments.Requests;
using BoslaPlatform.Application.Interfaces.Payments;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Settings;
using BoslaPlatform.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.API.Controllers.v1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IAppDbContext _context;
        private readonly StripeSettings _stripeSettings;

        public PaymentsController(
            IPaymentService paymentService,
            IAppDbContext context,
            IOptions<StripeSettings> stripeSettings)
        {
            _paymentService = paymentService;
            _context = context;
            _stripeSettings = stripeSettings.Value;
        }

        [HttpPost]
        public async Task<IActionResult> Initiate([FromBody] InitiatePaymentRequest request)
        {
            var result = await _paymentService.InitiateAsync(request, HttpContext.RequestAborted);

            if (result.IsError)
            {
                return BadRequest(result.ToApiResponse<PaymentResponseDto>());
            }
            return Ok(result.ToApiResponse<PaymentResponseDto>());
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _paymentService.GetByIdAsync(id, HttpContext.RequestAborted);
            if (result.IsError) return BadRequest(result.ToApiResponse<PaymentResponseDto>());
            return Ok(result.ToApiResponse<PaymentResponseDto>());
        }

        [HttpGet("appointments/{appointmentId:guid}/payment")]
        public async Task<IActionResult> GetByAppointment(Guid appointmentId)
        {
            var result = await _paymentService.GetByAppointmentAsync(appointmentId, HttpContext.RequestAborted);
            if (result.IsError) return BadRequest(result.ToApiResponse<PaymentResponseDto>());
            return Ok(result.ToApiResponse<PaymentResponseDto>());
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyPayments()
        {
            var result = await _paymentService.GetMyPaymentsAsync(HttpContext.RequestAborted);
            if (result.IsError) return BadRequest(result.ToApiResponse<IReadOnlyList<PaymentResponseDto>>());
            return Ok(result.ToApiResponse<IReadOnlyList<PaymentResponseDto>>());
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> ConfirmWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _stripeSettings.WebhookSecret
                );

                switch (stripeEvent.Type)
                {
                    case EventTypes.PaymentIntentSucceeded:
                        await HandlePaymentIntentSucceeded(stripeEvent);
                        break;
                    case EventTypes.PaymentIntentPaymentFailed:
                        await HandlePaymentIntentFailed(stripeEvent);
                        break;
                    case EventTypes.PaymentIntentCanceled:
                        await HandlePaymentIntentCanceled(stripeEvent);
                        break;
                    case "charge.refunded":
                        await HandleChargeRefunded(stripeEvent);
                        break;
                    case "charge.dispute.created":
                        await HandleDisputeCreated(stripeEvent);
                        break;
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task HandlePaymentIntentSucceeded(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null) return;

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.ExternalPaymentId == paymentIntent.Id);
            if (payment == null) return;

            var expectedAmount = (long)Math.Round(payment.Amount * 100, MidpointRounding.AwayFromZero);
            if (paymentIntent.Amount != expectedAmount)
            {
                return;
            }

            payment.Complete(paymentIntent.Id, paymentIntent.PaymentMethodId ?? "Card");

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == payment.AppointmentId);

            if (appointment != null && appointment.Status is AppointmentStatus.Pending or AppointmentStatus.Confirmed)
            {
                appointment.MarkAsPaid();
            }

            await _context.SaveChangesAsync();
        }

        private async Task HandlePaymentIntentFailed(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null) return;

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.ExternalPaymentId == paymentIntent.Id);
            if (payment == null) return;

            payment.MarkAsFailed(paymentIntent.LastPaymentError?.Message ?? "Payment failed");
            await _context.SaveChangesAsync();
        }

        private async Task HandlePaymentIntentCanceled(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null) return;

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.ExternalPaymentId == paymentIntent.Id);
            if (payment == null) return;

            payment.MarkAsFailed("Payment was canceled");
            await _context.SaveChangesAsync();
        }

        private async Task HandleChargeRefunded(Event stripeEvent)
        {
            var charge = stripeEvent.Data.Object as Charge;
            if (charge == null || string.IsNullOrEmpty(charge.PaymentIntentId)) return;

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.ExternalPaymentId == charge.PaymentIntentId);
            if (payment == null) return;

            payment.MarkAsRefunded(charge.Refunds?.Data?.FirstOrDefault()?.Reason ?? "Refunded via Stripe");
            await _context.SaveChangesAsync();
        }

        private async Task HandleDisputeCreated(Event stripeEvent)
        {
            var dispute = stripeEvent.Data.Object as Dispute;
            if (dispute == null) return;

            var paymentIntentId = dispute.PaymentIntentId;
            if (string.IsNullOrEmpty(paymentIntentId)) return;

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.ExternalPaymentId == paymentIntentId);
            if (payment == null) return;

            payment.MarkAsFailed($"Dispute filed: {dispute.Reason}");
            await _context.SaveChangesAsync();
        }
    }
}
