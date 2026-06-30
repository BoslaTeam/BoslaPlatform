using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BoslaPlatform.Shared;
using BoslaPlatform.Application.Features.Admin.DTOs;

namespace BoslaPlatform.Application.Features.Admin.Services
{
    public interface IAdminService
    {
        // ── Users ──
        Task<Result<BoslaPlatform.Shared.PaginatedList<UserDto>>> ListUsersAsync(int page = 1, int pageSize = 20, string? search = null, int? role = null, bool? isActive = null, CancellationToken cancellationToken = default);

        Task<Result<UserDetailsDto>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> CreateUserAsync(BoslaPlatform.Application.Features.Admin.Requests.CreateUserRequest request, CancellationToken cancellationToken = default);

        Task<Result> UpdateUserAsync(Guid userId, BoslaPlatform.Application.Features.Admin.Requests.UpdateUserRequest request, CancellationToken cancellationToken = default);

        Task<Result> UpdateUserRolesAsync(Guid userId, List<string> roles, CancellationToken cancellationToken = default);

        Task<Result> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> ReactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

        // ── Specialists ──
        Task<Result<BoslaPlatform.Shared.PaginatedList<AdminSpecialistListItemDto>>> ListSpecialistsAsync(int page = 1, int pageSize = 20, string? search = null, string? verificationStatus = null, CancellationToken cancellationToken = default);

        Task<Result<BoslaPlatform.Shared.PaginatedList<AdminSpecialistListItemDto>>> ListPendingSpecialistsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        Task<Result<AdminSpecialistDetailDto>> GetSpecialistDetailAsync(Guid specialistId, CancellationToken cancellationToken = default);

        Task<Result> VerifySpecialistAsync(Guid specialistId, bool isVerified, Guid verifiedByUserId, CancellationToken cancellationToken = default);

        Task<Result> UpdateSpecialistStatusAsync(Guid specialistId, string status, Guid? verifiedByUserId, CancellationToken cancellationToken = default);

        // ── Appointments ──
        Task<Result<PaginatedList<AdminAppointmentDto>>> ListAppointmentsAsync(int page = 1, int pageSize = 20, string? search = null, int? status = null, CancellationToken cancellationToken = default);

        Task<Result<AdminAppointmentDetailDto>> GetAppointmentDetailAsync(Guid appointmentId, CancellationToken cancellationToken = default);

        Task<Result> CancelAppointmentAsync(Guid appointmentId, string reason, CancellationToken cancellationToken = default);

        Task<Result> ConfirmAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);

        Task<Result> CompleteAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);

        // ── Lookups (Expertise, Skills, Tools) ──
        Task<Result<List<BoslaPlatform.Application.Features.Lookup.Response.LookupItemResponse>>> GetExpertiseListAsync(CancellationToken cancellationToken = default);

        Task<Result<Guid>> CreateExpertiseAsync(string name, CancellationToken cancellationToken = default);

        Task<Result> UpdateExpertiseAsync(Guid id, string name, CancellationToken cancellationToken = default);

        Task<Result> DeleteExpertiseAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Result<List<BoslaPlatform.Application.Features.Lookup.Response.LookupItemResponse>>> GetSkillListAsync(CancellationToken cancellationToken = default);

        Task<Result<Guid>> CreateSkillAsync(string name, CancellationToken cancellationToken = default);

        Task<Result> UpdateSkillAsync(Guid id, string name, CancellationToken cancellationToken = default);

        Task<Result> DeleteSkillAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Result<List<BoslaPlatform.Application.Features.Lookup.Response.LookupItemResponse>>> GetToolListAsync(CancellationToken cancellationToken = default);

        Task<Result<Guid>> CreateToolAsync(string name, CancellationToken cancellationToken = default);

        Task<Result> UpdateToolAsync(Guid id, string name, CancellationToken cancellationToken = default);

        Task<Result> DeleteToolAsync(Guid id, CancellationToken cancellationToken = default);

        // ── Payments ──
        Task<Result<PaginatedList<AdminPaymentDto>>> ListPaymentsAsync(int page = 1, int pageSize = 20, string? search = null, string? status = null, CancellationToken cancellationToken = default);

        Task<Result<AdminPaymentDetailDto>> GetPaymentDetailAsync(Guid paymentId, CancellationToken cancellationToken = default);

        Task<Result> RefundPaymentAsync(Guid paymentId, string? reason, CancellationToken cancellationToken = default);

        // ── System ──
        Task<Result<List<AuditLogDto>>> GetAuditLogsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        Task<Result<AdminDashboardDto>> GetDashboardStatsAsync(CancellationToken cancellationToken = default);
    }
}
