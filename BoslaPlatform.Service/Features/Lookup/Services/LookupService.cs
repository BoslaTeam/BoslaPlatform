using BoslaPlatform.Application.Features.Lookup.Response;
using BoslaPlatform.Application.Interfaces.Lookup;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.Features.Lookup.Services
{
    public class LookupService(IAppDbContext context) : ILookupService
    {
        public async Task<Result<List<LookupItemResponse>>> GetExpertiseAsync(CancellationToken ct = default)
        {
            var items = await context.Expertises
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemResponse(x.Id, x.Name))
                .ToListAsync(ct);

            return items;
        }

        public async Task<Result<List<LookupItemResponse>>> GetIndustriesAsync(CancellationToken ct = default)
        {
            var items = await context.Industries
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemResponse(x.Id, x.Name))
                .ToListAsync(ct);

            return items;
        }

        public async Task<Result<List<LookupItemResponse>>> GetSkillsAsync(CancellationToken ct = default)
        {
            var items = await context.Skills
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemResponse(x.Id, x.Name))
                .ToListAsync(ct);

            return items;
        }

        public async Task<Result<List<LookupItemResponse>>> GetToolsAsync(CancellationToken ct = default)
        {
            var items = await context.Tools
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemResponse(x.Id, x.Name))
                .ToListAsync(ct);

            return items;
        }
    }
}
