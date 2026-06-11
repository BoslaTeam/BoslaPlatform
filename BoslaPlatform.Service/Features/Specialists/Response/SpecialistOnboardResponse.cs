using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Specialists.Response
{
    public sealed record SpecialistOnboardResponse(
    Guid SpecialistId,
    VerificationStatus VerificationStatus
);
}
