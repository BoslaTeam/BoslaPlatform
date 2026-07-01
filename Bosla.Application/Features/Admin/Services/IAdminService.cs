using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BoslaPlatform.Shared;
using Bosla.Application.Features.Admin.DTOs;

namespace Bosla.Application.Features.Admin.Services
{
    public interface IAdminService
    {
        Task<Result<BoslaPlatform.Shared.PaginatedList<UserDto>>> ListUsersAsync(int page = 1, int pageSize = 20, string? search = null, int? role = null, bool? isActive = null, CancellationToken cancellationToken = default);

        Task<Result<UserDetailsDto>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> CreateUserAsync(BoslaPlatform.Application.Features.Admin.Requests.CreateUserRequest request, CancellationToken cancellationToken = default);

        Task<Result> UpdateUserAsync(Guid userId, BoslaPlatform.Application.Features.Admin.Requests.UpdateUserRequest request, CancellationToken cancellationToken = default);

        Task<Result> UpdateUserRolesAsync(Guid userId, List<string> roles, CancellationToken cancellationToken = default);

        Task<Result> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> ReactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> VerifySpecialistAsync(Guid specialistId, bool isVerified, CancellationToken cancellationToken = default);

        Task<Result<List<AuditLogDto>>> GetAuditLogsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        Task<Result<List<SpecialistDto>>> GetPendingSpecialistsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        Task<Result<SpecialistDetailsDto>> GetSpecialistDetailAsync(Guid specialistId, CancellationToken cancellationToken = default);

        Task<Result<List<AppointmentDto>>> GetAllAppointmentsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        Task<Result> CancelAppointmentAsync(Guid appointmentId, string reason, CancellationToken cancellationToken = default);

        Task<Result> RescheduleAppointmentAsync(Guid appointmentId, DateTime newStart, DateTime newEnd, CancellationToken cancellationToken = default);

        Task<Result<List<PaymentDto>>> GetAllPaymentsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        Task<Result> RefundPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);

        Task<Result<AuditLogDto>> GetAuditLogByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Result<DashboardDto>> GetDashboardAsync(CancellationToken cancellationToken = default);
    }
}
