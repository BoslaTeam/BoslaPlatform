using BoslaPlatform.Application.Features.Specialists.DTOs;

namespace BoslaPlatform.Application.Features.Specialists.Request
{
    public sealed class AddExperiencesRequest
    {
        public IReadOnlyList<AddExperienceRequestDTO> Experiences { get; init; } = [];
    }
}
