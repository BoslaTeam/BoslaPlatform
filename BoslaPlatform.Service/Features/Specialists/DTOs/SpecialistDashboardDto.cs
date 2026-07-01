using System;
using System.Collections.Generic;
using System.Text;

namespace BoslaPlatform.Application.Features.Specialists.DTOs
{
    public class SpecialistDashboardDto
    {
        public decimal MonthlyEarnings { get; init; } = 0;

        public double EarningsGrowthPercentage { get; init; }=0;

        public int UpcomingAppointments { get; init; } = 0;
        public double UpcomingAppointmentsGrowthPercentage { get; init; }

        public int CompletedAppointments { get; init; } = 0;

        public double CompletedAppointmentsGrowthPercentage { get; init; }

        public double AverageRating { get; init; } = 0;
        public double AverageRatingGrowthPercentage { get; init; }

        public int TotalReviews { get; set; } = 0;

        public IReadOnlyCollection<MonthlyRevenueDto> MonthlyRevenue { get; init; }= [];
            

        public IReadOnlyCollection<UpcomingAppointmentDto> UpcomingAppointmentsList { get; init; }= [];
            
    }
}
