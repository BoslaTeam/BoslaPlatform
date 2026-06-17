using System.Collections.Generic;

namespace BoslaPlatform.Shared
{
    public class PaginatedList<T>
    {
        public IReadOnlyCollection<T> Items { get; }
        public PaginationMetadata Metadata { get; }

        public PaginatedList(IReadOnlyCollection<T> items, PaginationMetadata metadata)
        {
            Items = items;
            Metadata = metadata;
        }
    }
}