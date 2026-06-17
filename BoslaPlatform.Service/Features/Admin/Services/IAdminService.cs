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
        Task<Result<List<UserDto>>> ListUsersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        Task<Result<UserDetailsDto>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> UpdateUserRolesAsync(Guid userId, List<string> roles, CancellationToken cancellationToken = default);

        Task<Result> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> ReactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<Result> VerifySpecialistAsync(Guid specialistId, bool isVerified, CancellationToken cancellationToken = default);

        Task<Result<List<AuditLogDto>>> GetAuditLogsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    }
}
