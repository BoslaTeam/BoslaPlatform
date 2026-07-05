using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.AI;

public class SmartRepliesResponse
{
    public List<string> Replies { get; set; } = [];
}

public class SessionPrepDto
{
    public string ClientName { get; set; } = string.Empty;
    public string? ClientTitle { get; set; }
    public string AppointmentTopic { get; set; } = string.Empty;
    public string AppointmentTime { get; set; } = string.Empty;
    public List<PastAppointmentDto> PastAppointments { get; set; } = [];
    public string? ConversationPreview { get; set; }
    public string? ExistingSummaryBrief { get; set; }
    public string? AiBrief { get; set; }
}

public class PastAppointmentDto
{
    public DateTimeOffset Date { get; set; }
    public string? Topic { get; set; }
    public string? Summary { get; set; }
}

public class DashboardInsightsDto
{
    public string TodaySummary { get; set; } = string.Empty;
    public string UpcomingSummary { get; set; } = string.Empty;
    public List<PendingActionDto> PendingActions { get; set; } = [];
    public string? AiTip { get; set; }
    public string? StatsBrief { get; set; }
}

public class PendingActionDto
{
    public Guid AppointmentId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
}

public interface ISpecialistAiService
{
    Task<Result<SmartRepliesResponse>> GetSmartRepliesAsync(Guid conversationId, CancellationToken ct = default);
    Task<Result<SessionPrepDto>> GetSessionPrepAsync(Guid appointmentId, CancellationToken ct = default);
    Task<Result<DashboardInsightsDto>> GetDashboardInsightsAsync(CancellationToken ct = default);
}
