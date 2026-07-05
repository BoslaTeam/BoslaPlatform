using System;
using System.Collections.Generic;

namespace BoslaPlatform.Application.Features.Portfolio.Requests
{
    public sealed record ReorderPortfolioRequest(List<ReorderItem> Items);

    public sealed record ReorderItem(Guid Id, int SortOrder);
}
