using System;
using System.Collections.Generic;

namespace BoslaPlatform.Application.Features.Admin.DTOs
{
    public class AdminDashboardDto
    {
        public int TotalUsers { get; set; }
        public int TotalSpecialists { get; set; }
        public int TotalAppointments { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingVerifications { get; set; }
        public int ActiveAppointments { get; set; }
        
        // Percentages (Growth from last month)
        public double UserGrowthPercentage { get; set; }
        public double RevenueGrowthPercentage { get; set; }
        public double AppointmentGrowthPercentage { get; set; }
        public double SpecialistGrowthPercentage { get; set; }
        
        // Lists for recent activities
        public List<UserDto> RecentUsers { get; set; } = new();
        public List<AdminAppointmentDto> RecentAppointments { get; set; } = new();
    }
}
