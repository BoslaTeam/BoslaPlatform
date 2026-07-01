using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Specialists.Response
{
    public sealed record VerificationStatusResponse(
        VerificationStatus Status,
        DateTime? SubmittedAt,
        DateTime? ReviewedAt,
        string? AdminNotes
    );
}
