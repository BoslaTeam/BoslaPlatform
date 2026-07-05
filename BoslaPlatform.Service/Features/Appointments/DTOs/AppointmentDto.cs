using System;
using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Appointments.DTOs
{
    public class AppointmentDto
    {
        public Guid Id { get; set; }
        public Guid SpecialistId { get; set; }
        public string SpecialistName { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public AppointmentStatus Status { get; set; }
        public string? SessionTopic { get; set; }
        public string? Notes { get; set; }
        public decimal SessionPrice { get; set; }

        public DateTimeOffset? ConfirmedAt { get; set; }
        public bool IsPaid { get; set; }
        public Guid? ConversationId { get; set; }
    }
}