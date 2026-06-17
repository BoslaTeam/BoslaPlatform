using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.Request
{
    public sealed record UpdateCancellationPolicyRequest(
      int CancellationNoticeHours,
      bool AllowCancellation,
      string? CancellationPolicy
  );
}
