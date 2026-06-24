namespace BoslaPlatform.Shared.Pagination
{
    public class PaginationRequest
    {
        private const int MaxPageSize = 100;
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        // Ensure the page number is at least 1
        public int NormalizePageNumber()
            => PageNumber <= 0 ? 1 : PageNumber;
        // Ensure the page size is within the allowed limits
        public int NormalizePageSize()
            => PageSize <= 0
                ? 10
                : Math.Min(PageSize, MaxPageSize);
    }
    public sealed class PaginatedResult<T>
    {
        public IReadOnlyCollection<T> Items { get; init; }

        public PaginationMetadata Metadata { get; init; }

        public PaginatedResult(
            IReadOnlyCollection<T> items,
            PaginationMetadata metadata)
        {
            Items = items;
            Metadata = metadata;
        }
    }
}
