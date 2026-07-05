namespace BoslaPlatform.Shared
{
    public sealed class PaginationMetadata
    {
        public int CurrentPage { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        private PaginationMetadata()
        {
        }

        public PaginationMetadata(
            int currentPage,
            int pageSize,
            int totalCount)
        {
            CurrentPage = currentPage;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = (int)Math.Ceiling(
                totalCount / (double)pageSize);
        }

        public static PaginationMetadata Create(
            int currentPage,
            int pageSize,
            int totalCount)
        {
            return new PaginationMetadata(
                currentPage,
                pageSize,
                totalCount);
        }
    }
}