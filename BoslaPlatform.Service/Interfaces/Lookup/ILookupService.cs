using BoslaPlatform.Application.Features.Lookup.Response;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Lookup
{
    public interface ILookupService
    {
        Task<Result<List<LookupItemResponse>>> GetExpertiseAsync(CancellationToken ct = default);

        Task<Result<List<LookupItemResponse>>> GetIndustriesAsync(CancellationToken ct = default);

        Task<Result<List<LookupItemResponse>>> GetSkillsAsync(CancellationToken ct = default);

        Task<Result<List<LookupItemResponse>>> GetToolsAsync(CancellationToken ct = default);
    }
}
