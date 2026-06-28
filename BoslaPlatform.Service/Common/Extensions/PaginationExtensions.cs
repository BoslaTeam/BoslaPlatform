using Microsoft.EntityFrameworkCore;
namespace BoslaPlatform.Shared.Pagination
{
    public static class PaginationExtensions
    {
        public static async Task<PaginatedResult<T>>
            ToPaginatedResultAsync<T>(
                this IQueryable<T> query,
                int pageNumber,
                int pageSize,
                CancellationToken ct = default)
        {
            var totalCount =
                await query.CountAsync(ct);

            var items =
                await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

            return new PaginatedResult<T>(
                items,
                PaginationMetadata.Create(
                    pageNumber,
                    pageSize,
                    totalCount));
        }
    }
}
