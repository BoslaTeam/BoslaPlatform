using System;
using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Appointments.DTOs
{
    public class AppointmentStatusHistoryDto
    {
        public Guid Id { get; set; }
        public AppointmentStatus OldStatus { get; set; }
        public AppointmentStatus NewStatus { get; set; }
        public DateTimeOffset ChangedAt { get; set; }
        public Guid ChangedBy { get; set; }
        public string? Reason { get; set; }

        public AppointmentStatusHistoryDto()
        {
        }

        public AppointmentStatusHistoryDto(Guid id, AppointmentStatus oldStatus, AppointmentStatus newStatus, DateTimeOffset changedAt, Guid changedBy, string? reason)
        {
            Id = id;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            ChangedAt = changedAt;
            ChangedBy = changedBy;
            Reason = reason;
        }
    }
}