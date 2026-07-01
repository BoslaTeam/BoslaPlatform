using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Specialists.Response
{
    public sealed record SpecialistDocumentResponse(
        Guid Id,
        SpecialistDocumentType Type,
        string Url,
        string OriginalFileName
    );
}
