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
                return BadRequest(result.Errors);
            }
            return Ok(result.Value);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _paymentService.GetByIdAsync(id, HttpContext.RequestAborted);
            if (result.IsError) return BadRequest(result.Errors);
            return Ok(result.Value);
        }

        [HttpGet("appointments/{appointmentId:guid}/payment")]
        public async Task<IActionResult> GetByAppointment(Guid appointmentId)
        {
            var result = await _paymentService.GetByAppointmentAsync(appointmentId, HttpContext.RequestAborted);
            if (result.IsError) return BadRequest(result.Errors);
            return Ok(result.Value);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyPayments()
        {
            var result = await _paymentService.GetMyPaymentsAsync(HttpContext.RequestAborted);
            if (result.IsError) return BadRequest(result.Errors);
            return Ok(result.Value);
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

                if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent != null)
                    {
                        var payment = await _context.Payments
                            .FirstOrDefaultAsync(p => p.ExternalPaymentId == paymentIntent.Id);

                        if (payment != null)
                        {
                            payment.Complete(paymentIntent.Id, paymentIntent.PaymentMethodId ?? "Card");

                            var appointment = await _context.Appointments
                                .FirstOrDefaultAsync(a => a.Id == payment.AppointmentId);

                            if (appointment != null && appointment.Status == AppointmentStatus.Pending)
                            {
                                appointment.MarkAsPaid();
                            }

                            await _context.SaveChangesAsync();
                        }
                    }
                }
                else if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent != null)
                    {
                        var payment = await _context.Payments
                            .FirstOrDefaultAsync(p => p.ExternalPaymentId == paymentIntent.Id);

                        if (payment != null)
                        {
                            payment.MarkAsFailed(paymentIntent.LastPaymentError?.Message ?? "Payment failed");
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
