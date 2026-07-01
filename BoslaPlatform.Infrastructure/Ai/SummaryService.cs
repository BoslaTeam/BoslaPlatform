using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Models;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Infrastructure.AI;

public class SummaryService : ISummaryService
{
    private readonly IAppDbContext _db;
    private readonly IChatService _chat;

    public SummaryService(IAppDbContext db, IChatService chat)
    {
        _db = db;
        _chat = chat;
    }

    public async Task<Result<SessionSummary>> GetAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var summary = await _db.Set<SessionSummary>().FirstOrDefaultAsync(s => s.AppointmentId == appointmentId, cancellationToken);
        if (summary != null && summary.Status == BoslaPlatform.Domain.Enums.SummaryStatus.Ready)
            return Result<SessionSummary>.Success(summary);

        // If not present or not ready, attempt to generate inline (best-effort)
        var regen = await RegenerateAsync(appointmentId, cancellationToken);
        if (!regen.IsSuccess)
            return Result<SessionSummary>.Failure(Error.NotFound("Summary.NotFound", "Summary not found and could not be generated."));

        var newSummary = await _db.Set<SessionSummary>().FirstOrDefaultAsync(s => s.AppointmentId == appointmentId, cancellationToken);
        if (newSummary == null)
            return Result<SessionSummary>.Failure(Error.NotFound("Summary.NotFound", "Summary not found after generation."));

        return Result<SessionSummary>.Success(newSummary);
    }

    public async Task<Result<bool>> RegenerateAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        // Fetch conversation messages for the appointment
        var conv = await _db.Set<BoslaPlatform.Domain.Models.Communication.Conversation>().Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId, cancellationToken);

        if (conv == null)
            return Result<bool>.Failure(Error.NotFound("Conversation.NotFound", "Conversation for appointment not found."));

        var messages = conv.Messages.OrderBy(m => m.CreatedAtUtc).Select(m => m.MessageText).ToList();
        var combined = string.Join("\n", messages);
        if (string.IsNullOrWhiteSpace(combined))
            return Result<bool>.Failure(Error.Validation("Summary.EmptySource", "No messages to summarize."));

        var prompt = $"Summarize the following session and extract key takeaways and action items:\n\n{combined}";
        string reply;
        try
        {
            reply = await _chat.ChatAsync(prompt, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(Error.Unexpected("Summary.GenerationFailed", ex.Message));
        }

        var summary = await _db.Set<SessionSummary>().FirstOrDefaultAsync(s => s.AppointmentId == appointmentId, cancellationToken);
        if (summary == null)
        {
            summary = new SessionSummary { AppointmentId = appointmentId };
            _db.Set<SessionSummary>().Add(summary);
        }

        summary.KeyTakeaways = reply;
        summary.LlmProvider = "Configured";
        summary.Status = BoslaPlatform.Domain.Enums.SummaryStatus.Ready;

        await _db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
