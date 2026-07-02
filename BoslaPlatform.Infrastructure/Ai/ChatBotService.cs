using BoslaPlatform.Application.Features.Specialists.DTOs;
using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Service.Features.AI.Requests;
using BoslaPlatform.Service.Features.AI.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Infrastructure.AI;

public class ChatBotService : IChatBotService
{
    private readonly IChatService _chat;
    private readonly IEmbeddingService _emb;
    private readonly IVectorStore _vectors;
    private readonly IAppDbContext _db;
    private readonly ILogger<ChatBotService> _logger;
    private readonly IUser _currentUser;

    private static string GetSystemPrompt(IUser user)
    {
        if (user.Role == "Specialist")
        {
            return @"أنت مساعد ذكي للمتخصصين في منصة بوصلة للاستشارات. اسمك بوصلة.
مهمتك الأساسية: مساعدة المتخصص في إدارة مواعيده، استشاراته، وملفه الشخصي.

تعليمات مهمة للتنسيق:
- استخدم النص فقط بدون أي تنسيق markdown أو HTML
- ممنوع استخدام ** أو * أو ` أو # أو أي رموز تنسيق
- استخدم سطر جديد بين كل فقرة

تعليمات مهمة للسلوك:
- أجب عن استفسارات المتخصص بخصوص المواعيد، الإشعارات، والعملاء، يمكنك إعلامه بأنه يمكنه مراجعة لوحة التحكم الخاصة به لمعرفة المواعيد.
- لا تقترح متخصصين آخرين عليه لأنه هو المتخصص!
- كن مهنياً ومختصراً، وأجب باللغة العربية.";
        }

        return @"أنت مساعد ذكي لمنصة بوصلة للاستشارات. اسمك بوصلة.
مهمتك الأساسية: مساعدة المستخدم في العثور على المتخصص المناسب لاحتياجاته.

تعليمات مهمة للتنسيق:
- استخدم النص فقط بدون أي تنسيق markdown أو HTML
- ممنوع استخدام ** أو * أو ` أو # أو أي رموز تنسيق
- استخدم سطر جديد بين كل فقرة
- ابدأ كل متخصص بشرطة - على سطر منفصل
- اكتب اسم المتخصص وتخصصه على نفس السطر بعد الشرطة

تعليمات مهمة للسلوك:
- إذا سلّم المستخدم أو حيّاك، رد عليه بترحيب ودود واسأله كيف تقدر تساعده. لا تعرض أي متخصصين.
- إذا سأل سؤال عام أو دردشة، أجب بشكل ودي ووجّهه للمنصة. لا تعرض أي متخصصين.
- فقط عندما يطلب المستخدم متخصصاً أو استشارة بشكل واضح، اعرض المتخصصين من قائمة النتائج (بعد علامة ===النتائج===).
- إذا طلب متخصصاً ولم تكن هناك نتائج (لا يوجد قسم ===النتائج===)، اعتذر واطلب منه توضيحاً أكثر.
- لا ترسل المستخدم إلى خاصية البحث—أنت هنا لتعرض النتائج مباشرة.
كن ودوداً ومختصراً، واجب باللغة العربية.";
    }

    // Common greeting patterns — skip specialist search entirely
    private static readonly string[] GreetingPatterns = {
        "سلام", "السلام", "مرحبا", "مرحباً", "أهلا", "أهلاً", "هلا", "هاي",
        "صباح", "مساء", "شكرا", "شكراً", "كيف حالك", "كيفك", "ازيك", "عامل ايه",
        "hello", "hi", "hey", "thanks", "thank you", "good morning", "good evening"
    };

    public ChatBotService(IChatService chat, IEmbeddingService emb, IVectorStore vectors, IAppDbContext db, ILogger<ChatBotService> logger, IUser currentUser)
    {
        _chat = chat;
        _emb = emb;
        _vectors = vectors;
        _db = db;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var msg = request.Message ?? string.Empty;
            var history = request.History ?? new List<ChatMessageDto>();

            // Detect greetings/small talk — don't waste an embedding call
            var isGreeting = IsGreetingOrSmallTalk(msg);
            var isSpecialist = _currentUser.Role == "Specialist";

            List<SpecialistListItemResponse> specialists;
            if (isGreeting || isSpecialist)
            {
                if (isGreeting)
                    _logger.LogInformation("Greeting detected, skipping search: {Msg}", msg);
                else
                    _logger.LogInformation("User is specialist, skipping specialist search: {Msg}", msg);

                specialists = [];
            }
            else
            {
                specialists = await SearchSpecialistsAsync(msg, cancellationToken);
            }

            // Temporarily use the null-checked msg and history
            request.Message = msg;
            request.History = history;

            var prompt = BuildPrompt(request, specialists, _currentUser);
            var reply = await _chat.ChatAsync(prompt, cancellationToken);
            return new ChatResponse { Reply = reply, Specialists = specialists };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in ChatAsync");
            return new ChatResponse { Reply = $"حدث خطأ في النظام: {ex.Message}\n{ex.StackTrace}", Specialists = [] };
        }
    }

    private static bool IsGreetingOrSmallTalk(string message)
    {
        var cleaned = message.Trim();
        if (cleaned.Length < 3) return true; // too short to be a real query

        var lower = cleaned.ToLowerInvariant();
        return GreetingPatterns.Any(g => lower.Contains(g, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<SpecialistListItemResponse>> SearchSpecialistsAsync(string query, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Searching specialists for: {Query}", query);

            // ── Step 1: Vector search ──
            var qEmb = await _emb.CreateEmbeddingAsync(query, ct);
            var hits = await _vectors.SearchSimilarAsync(qEmb, topK: 6, ct);
            _logger.LogInformation("Vector hits: {Count}, scores: [{Scores}]",
                hits.Count, string.Join(", ", hits.Select(h => $"{h.Score:F2}")));

            var ids = hits
                .Where(h => h.Score >= 0.60f)
                .Select(h => h.SpecialistId)
                .Distinct()
                .ToList();

            if (ids.Count > 0)
            {
                var specialists = await _db.Set<Specialist>()
                    .AsSplitQuery()
                    .Where(s => ids.Contains(s.Id))
                    .Include(s => s.User)
                    .Include(s => s.Reviews)
                    .Include(s => s.Verification)
                    .ToListAsync(ct);

                var results = specialists
                    .DistinctBy(s => s.UserId)
                    .Select(MapToResponse)
                    .ToList();

                _logger.LogInformation("Vector matched {Count} unique specialists", results.Count);
                return results;
            }

            // ── Step 2: Keyword fallback ──
            _logger.LogInformation("No vector matches, trying keyword search");

            var terms = query
                .Split(new[] { ' ', '،', ',', '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 2)
                .ToList();

            if (terms.Count == 0)
                return [];

            var allSpecialists = await _db.Set<Specialist>()
                .AsSplitQuery()
                .Include(s => s.User)
                .Include(s => s.SpecialistSkills)!.ThenInclude(ss => ss.Skill)
                .Include(s => s.SpecialistExpertise)!.ThenInclude(se => se.Expertise)
                .Include(s => s.Reviews)
                .Include(s => s.Verification)
                .Where(s => s.User != null)
                .ToListAsync(ct);

            var allTerms = new[] { query }.Concat(terms).ToList();

            var matched = allSpecialists
                .DistinctBy(s => s.UserId)
                .Where(s =>
                    allTerms.Any(term =>
                        (s.User?.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) ||
                        (s.User?.Title?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) ||
                        (s.User?.Bio?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) ||
                        (s.SpecialistSkills?.Any(ss => ss.Skill?.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) == true) ||
                        (s.SpecialistExpertise?.Any(se => se.Expertise?.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) == true)
                    )
                )
                .Take(5)
                .ToList();

            _logger.LogInformation("Keyword matched {Count} specialists", matched.Count);

            // Return only actual matches — NO "show all" fallback
            return matched.Select(MapToResponse).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for: {Query}", query);
            return [];
        }
    }

    private static SpecialistListItemResponse MapToResponse(Specialist s) => new()
    {
        Id = s.Id,
        Name = s.User?.Name ?? string.Empty,
        Title = s.User?.Title,
        ProfileImageUrl = s.User?.ProfileImageUrl,
        HourlyRate = s.HourlyRate,
        ExperienceLevel = s.ExperienceLevel,
        VerificationStatus = s.Verification?.Status ?? Domain.Enums.VerificationStatus.Pending,
        Rating = s.Reviews?.Any() == true ? Math.Round((decimal)s.Reviews.Average(r => r.Rating), 1) : 0,
        IsOnline = false
    };

    private static string BuildPrompt(ChatRequest request, List<SpecialistListItemResponse> specialists, IUser user)
    {
        var parts = new List<string> { GetSystemPrompt(user), "---" };
        foreach (var msg in request.History)
        {
            parts.Add($"{msg.Role}: {msg.Content}");
        }
        parts.Add($"user: {request.Message}");

        if (specialists.Count > 0)
        {
            parts.Add("");
            parts.Add("===النتائج===");
            foreach (var s in specialists)
            {
                parts.Add($"- {s.Name} | {s.Title} | التقييم: {s.Rating} | السعر: {s.HourlyRate}/س | الخبرة: {s.ExperienceLevel}");
            }
        }

        parts.Add("assistant:");
        return string.Join("\n", parts);
    }
}
