namespace BoslaPlatform.Application.Features.Admin.DTOs
{
    public sealed class DashboardDto
    {
        public int TotalUsers { get; set; }
        public int TotalSpecialists { get; set; }
        public int PendingSpecialists { get; set; }
        public int TotalAppointments { get; set; }
        public decimal TotalPayments { get; set; }
    }
}
