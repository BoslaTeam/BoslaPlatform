using System.Data;
using System.Threading;
using System.Threading.Tasks;
using BoslaPlatform.Application.Features.Admin.DTOs;
using BoslaPlatform.Application.Features.Admin.Repositories;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BoslaPlatform.Infrastructure.Data
{
    public class DapperDashboardRepository : IDashboardRepository
    {
        private readonly string _connectionString;

        public DapperDashboardRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            const string sql = @"
SELECT
    (SELECT COUNT(*) FROM [Users]) AS TotalUsers,
    (SELECT COUNT(*) FROM [Specialists]) AS TotalSpecialists,
    (SELECT COUNT(*) FROM [Specialists] WHERE [VerificationStatus] = 0) AS PendingSpecialists,
    (SELECT COUNT(*) FROM [Appointments]) AS TotalAppointments,
    (SELECT ISNULL(SUM([Amount]),0) FROM [Payments]) AS TotalPayments;";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var result = await conn.QuerySingleAsync(sql);

            return new DashboardDto
            {
                TotalUsers = (int)result.TotalUsers,
                TotalSpecialists = (int)result.TotalSpecialists,
                PendingSpecialists = (int)result.PendingSpecialists,
                TotalAppointments = (int)result.TotalAppointments,
                TotalPayments = (decimal)result.TotalPayments
            };
        }
    }
}
