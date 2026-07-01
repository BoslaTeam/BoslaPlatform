using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Specialists.Response
{
    public sealed record StartResponse(
        Guid SpecialistId,
        VerificationStatus VerificationStatus
    );
}
