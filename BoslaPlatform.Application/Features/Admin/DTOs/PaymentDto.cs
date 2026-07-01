using System;

namespace BoslaPlatform.Application.Features.Admin.DTOs
{
    public sealed class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid AppointmentId { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
