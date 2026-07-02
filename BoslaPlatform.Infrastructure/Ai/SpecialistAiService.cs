using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.AI;

public class SpecialistAiService : ISpecialistAiService
{
    private readonly IAppDbContext _db;
    private readonly IChatService _chat;
    private readonly ILogger<SpecialistAiService> _logger;
    private readonly BoslaPlatform.Application.Interfaces.Authentication.IUser _currentUser;

    public SpecialistAiService(
        IAppDbContext db,
        IChatService chat,
        ILogger<SpecialistAiService> logger,
        BoslaPlatform.Application.Interfaces.Authentication.IUser currentUser)
    {
        _db = db;
        _chat = chat;
        _logger = logger;
        _currentUser = currentUser;
    }

    // ─── Smart Replies ───────────────────────────────────────────────

    public async Task<Result<SmartRepliesResponse>> GetSmartRepliesAsync(Guid conversationId, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Id == null)
            return Result<SmartRepliesResponse>.Failure(Error.Unauthorized("SmartReply.Unauthenticated", "User is not authenticated."));

        var specialist = await _db.Set<Specialist>().FirstOrDefaultAsync(s => s.UserId == _currentUser.Id.Value, ct);
        if (specialist == null)
            return Result<SmartRepliesResponse>.Failure(Error.Forbidden("SmartReply.NotSpecialist", "User is not a specialist."));

        var conv = await _db.Set<BoslaPlatform.Domain.Models.Communication.Conversation>()
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAtUtc).Take(10))
                .ThenInclude(m => m.Sender)
            .Include(c => c.Participants).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (conv == null)
            return Result<SmartRepliesResponse>.Failure(Error.NotFound("SmartReply.ConversationNotFound", "Conversation not found."));

        if (!conv.Participants.Any(p => p.UserId == _currentUser.Id.Value))
            return Result<SmartRepliesResponse>.Failure(Error.Forbidden("SmartReply.NotParticipant", "User is not a participant in this conversation."));

        var messages = conv.Messages
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => $"{m.Sender.Name}: {m.MessageText}")
            .ToList();

        var history = string.Join("\n", messages);
        if (string.IsNullOrWhiteSpace(history))
            return Result<SmartRepliesResponse>.Success(new SmartRepliesResponse { Replies = ["كيف أقدر أساعدك اليوم؟"] });

        var prompt = $@"أنت متخصص في منصة بوصلة للاستشارات. هذه محادثة بينك وبين عميل.
اقترح 3 ردود قصيرة ومناسبة يمكنك إرسالها الآن.
اكتب الردود فقط، كل رد في سطر منفصل، بدون ترقيم أو تنسيق.

المحادثة:
{history}

الردود المقترحة:";

        try
        {
            var reply = await _chat.ChatAsync(prompt, ct);
            var replies = reply.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim().TrimStart("1234567890-.* ".ToCharArray()))
                .Where(l => l.Length > 0)
                .Take(3)
                .ToList();

            while (replies.Count < 3)
                replies.Add("شكراً لك، سأتواصل معك قريباً.");

            return Result<SmartRepliesResponse>.Success(new SmartRepliesResponse { Replies = replies });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Smart reply generation failed for conversation {ConvId}", conversationId);
            return Result<SmartRepliesResponse>.Success(new SmartRepliesResponse
            {
                Replies = ["شكراً لتواصلك، سأراجع طلبك.", "حسناً، تم.", "سأتواصل معك قريباً."]
            });
        }
    }

    // ─── Session Prep ────────────────────────────────────────────────

    public async Task<Result<SessionPrepDto>> GetSessionPrepAsync(Guid appointmentId, CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Id == null)
            return Result<SessionPrepDto>.Failure(Error.Unauthorized("SessionPrep.Unauthenticated", "User is not authenticated."));

        var specialist = await _db.Set<Specialist>().FirstOrDefaultAsync(s => s.UserId == _currentUser.Id.Value, ct);
        if (specialist == null)
            return Result<SessionPrepDto>.Failure(Error.Forbidden("SessionPrep.NotSpecialist", "User is not a specialist."));

        var appointment = await _db.Set<Appointment>()
            .Include(a => a.User)
            .Include(a => a.SessionSummary)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, ct);

        if (appointment == null)
            return Result<SessionPrepDto>.Failure(Error.NotFound("SessionPrep.AppointmentNotFound", "Appointment not found."));

        if (appointment.SpecialistId != specialist.Id)
            return Result<SessionPrepDto>.Failure(Error.Forbidden("SessionPrep.NotYourAppointment", "This appointment does not belong to you."));

        // Past appointments with same client
        var pastApps = await _db.Set<Appointment>()
            .Where(a => a.UserId == appointment.UserId && a.SpecialistId == specialist.Id && a.Status == Domain.Enums.AppointmentStatus.Completed)
            .Include(a => a.SessionSummary)
            .OrderByDescending(a => a.End)
            .Take(5)
            .ToListAsync(ct);

        // Conversation preview
        var conv = await _db.Set<BoslaPlatform.Domain.Models.Communication.Conversation>()
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAtUtc).Take(5))
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId, ct);

        var conversationPreview = conv?.Messages?
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => $"{m.Sender.Name}: {m.MessageText}")
            .ToList();

        var dto = new SessionPrepDto
        {
            ClientName = appointment.User?.Name ?? "عميل",
            ClientTitle = appointment.User?.Title,
            AppointmentTopic = appointment.SessionTopic ?? "استشارة",
            AppointmentTime = appointment.Start.LocalDateTime.ToString("dd/MM/yyyy hh:mm tt"),
            PastAppointments = pastApps.Select(a => new PastAppointmentDto
            {
                Date = a.Start,
                Topic = a.SessionTopic,
                Summary = a.SessionSummary?.KeyTakeaways
            }).ToList(),
            ConversationPreview = conversationPreview != null ? string.Join("\n", conversationPreview) : null,
            ExistingSummaryBrief = appointment.SessionSummary?.KeyTakeaways
        };

        // Generate AI brief
        try
        {
            var pastSummary = pastApps.Any() && pastApps.First().SessionSummary != null
                ? $"آخر جلسة سابقة: {pastApps.First().SessionSummary.KeyTakeaways}"
                : "لا توجد جلسات سابقة.";

            var convText = conversationPreview != null
                ? $"آخر المحادثة:\n{string.Join("\n", conversationPreview.TakeLast(3))}"
                : "لا توجد محادثة سابقة.";

            var briefPrompt = $@"أنت مساعد متخصص في منصة بوصلة. حضّر ملخصاً سريعاً للمتخصص قبل بدء جلسة الفيديو مع العميل.

العميل: {dto.ClientName}
موضوع الجلسة: {dto.AppointmentTopic}
{pastSummary}
{convText}

اكتب 3-4 أسطر بالعربية تلخص فيها وضع العميل والمحاور المتوقعة للجلسة:";

            dto.AiBrief = await _chat.ChatAsync(briefPrompt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI brief generation failed for appointment {AppointmentId}", appointmentId);
            dto.AiBrief = null;
        }

        return Result<SessionPrepDto>.Success(dto);
    }

    // ─── Dashboard Insights ──────────────────────────────────────────

    public async Task<Result<DashboardInsightsDto>> GetDashboardInsightsAsync(CancellationToken ct = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Id == null)
            return Result<DashboardInsightsDto>.Failure(Error.Unauthorized("DashboardInsights.Unauthenticated", "User is not authenticated."));

        var specialist = await _db.Set<Specialist>().FirstOrDefaultAsync(s => s.UserId == _currentUser.Id.Value, ct);
        if (specialist == null)
            return Result<DashboardInsightsDto>.Failure(Error.Forbidden("DashboardInsights.NotSpecialist", "User is not a specialist."));

        var now = DateTimeOffset.UtcNow;
        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);

        // Today's appointments
        var todayApps = await _db.Set<Appointment>()
            .Where(a => a.SpecialistId == specialist.Id && a.Start >= todayStart && a.Start < todayEnd)
            .Include(a => a.User)
            .OrderBy(a => a.Start)
            .ToListAsync(ct);

        // Upcoming appointments (next 7 days, excluding today)
        var weekApps = await _db.Set<Appointment>()
            .Where(a => a.SpecialistId == specialist.Id && a.Start > todayEnd && a.Start < todayEnd.AddDays(7))
            .OrderBy(a => a.Start)
            .ToListAsync(ct);

        // Pending confirmations
        var pendingApps = await _db.Set<Appointment>()
            .Where(a => a.SpecialistId == specialist.Id && a.Status == Domain.Enums.AppointmentStatus.Pending)
            .Include(a => a.User)
            .OrderBy(a => a.Start)
            .Take(5)
            .ToListAsync(ct);

        // Stats for brief
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
        var lastMonthStart = monthStart.AddMonths(-1);
        var thisMonthApps = await _db.Set<Appointment>()
            .CountAsync(a => a.SpecialistId == specialist.Id && a.CreatedAtUtc >= monthStart, ct);
        var lastMonthApps = await _db.Set<Appointment>()
            .CountAsync(a => a.SpecialistId == specialist.Id && a.CreatedAtUtc >= lastMonthStart && a.CreatedAtUtc < monthStart, ct);

        var dto = new DashboardInsightsDto
        {
            PendingActions = pendingApps.Select(a => new PendingActionDto
            {
                AppointmentId = a.Id,
                ClientName = a.User?.Name ?? "عميل",
                Action = "accept/reject",
                Time = a.Start.LocalDateTime.ToString("dd/MM hh:mm tt")
            }).ToList()
        };

        // Generate summaries
        try
        {
            var todayCount = todayApps.Count;
            var todayTimes = todayApps.Any()
                ? string.Join("، ", todayApps.Select(a => $"{a.User?.Name ?? "عميل"} في {a.Start.LocalDateTime:hh:mm tt}"))
                : "لا توجد";

            dto.TodaySummary = todayCount > 0
                ? $"لديك {todayCount} مواعيد اليوم: {todayTimes}."
                : "لا توجد مواعيد اليوم. يمكنك استغلال الوقت لمتابعة طلباتك.";

            var weekCount = weekApps.Count;
            dto.UpcomingSummary = weekCount > 0
                ? $"لديك {weekCount} مواعيد الأسبوع القادم."
                : "لا توجد مواعيد للأسبوع القادم.";

            if (lastMonthApps > 0)
            {
                var growth = ((double)thisMonthApps - lastMonthApps) / lastMonthApps * 100;
                dto.StatsBrief = growth switch
                {
                    > 0 => $"عدد الحجوزات هذا الشهر أعلى بنسبة {growth:F0}% مقارنة بالشهر الماضي.",
                    < 0 => $"عدد الحجوزات هذا الشهر أقل بنسبة {Math.Abs(growth):F0}% مقارنة بالشهر الماضي.",
                    _ => "عدد الحجوزات مستقر مقارنة بالشهر الماضي."
                };
            }

            var insightPrompt = $@"أنت مساعد متخصص لمنصة بوصلة. بناءً على بيانات المتخصص التالية، قدم نصيحة مختصرة (سطر واحد) بالعربية:
- مواعيد اليوم: {todayCount}
- مواعيد الأسبوع: {weekCount}
- مواعيد معلقة: {pendingApps.Count}

النصيحة:";

            dto.AiTip = await _chat.ChatAsync(insightPrompt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dashboard insights AI generation failed");
        }

        return Result<DashboardInsightsDto>.Success(dto);
    }
}
