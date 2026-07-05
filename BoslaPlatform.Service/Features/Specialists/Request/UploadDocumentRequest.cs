using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Specialists.Request
{
    public sealed class UploadDocumentRequest
    {
        public SpecialistDocumentType Type { get; init; }
        public string Url { get; init; } = string.Empty;
        public string OriginalFileName { get; init; } = string.Empty;
    }
}
