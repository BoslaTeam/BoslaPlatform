using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.DTOs
{
    public class MonthlyRevenueDto
    {
        public string Month { get; init; } = string.Empty;

        public decimal Amount { get; init; }
    }
}
