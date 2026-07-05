namespace BoslaPlatform.Application.Features.Admin.DTOs;

public sealed class AdminAppointmentDetailDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string? UserAvatarUrl { get; set; }
    public Guid SpecialistId { get; set; }
    public string SpecialistName { get; set; } = string.Empty;
    public string? SpecialistAvatarUrl { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SessionTopic { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AdminAppointmentStatusHistoryDto> StatusHistory { get; set; } = [];
    public string? KeyTakeaways { get; set; }
    public string? ActionItemsForUser { get; set; }
    public string? ActionItemsForSpec { get; set; }
}

public sealed class AdminAppointmentStatusHistoryDto
{
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
