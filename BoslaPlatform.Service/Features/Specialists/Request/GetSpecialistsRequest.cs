using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Shared.Pagination;

namespace BoslaPlatform.Application.Features.Specialists.Request
{
    public sealed class GetSpecialistsRequest : PaginationRequest
    {
        public string? SearchTerm { get; init; }

        public ExperienceLevel? ExperienceLevel { get; init; }

        public decimal? MinHourlyRate { get; init; }

        public decimal? MaxHourlyRate { get; init; }

        public Guid? SkillId { get; init; }

        public Guid? ToolId { get; init; }

        public Guid? ExpertiseId { get; init; }
    }
}
