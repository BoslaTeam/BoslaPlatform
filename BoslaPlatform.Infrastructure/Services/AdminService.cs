using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoslaPlatform.Application.Features.Admin.DTOs;
using BoslaPlatform.Application.Features.Admin.Services;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models;
using BoslaPlatform.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAppDbContext _context;
        private readonly UserManager<User> _userManager;
        public AdminService(IAppDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Result<List<UserDto>>> ListUsersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var skip = (page - 1) * pageSize;
            var users = await _userManager.Users
                .OrderBy(u => u.CreatedAtUtc)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = new List<UserDto>();
            foreach (var u in users)
            {
                var dto = new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.Name,
                    IsActive = u.IsActive
                };

                var roles = await _userManager.GetRolesAsync(u);
                dto.Roles = roles.ToArray();
                dtos.Add(dto);
            }

            return Result<List<UserDto>>.Success(dtos);
        }

        public async Task<Result<UserDetailsDto>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Error.NotFound(description: "User not found.");

            var dto = new UserDetailsDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.Name,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAtUtc.UtcDateTime
            };
            var roles = await _userManager.GetRolesAsync(user);
            dto.Roles = roles.ToArray();
            return Result<UserDetailsDto>.Success(dto);
        }

        public async Task<Result> UpdateUserRolesAsync(Guid userId, List<string> roles, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Error.NotFound(description: "User not found.");

            var currentRoles = await _userManager.GetRolesAsync(user);
            var toAdd = roles.Except(currentRoles).ToList();
            var toRemove = currentRoles.Except(roles).ToList();

            if (toRemove.Any())
            {
                var remResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
                if (!remResult.Succeeded)
                    return remResult.Errors.Select(e => Error.Create(ErrorKind.Validation, e.Code, e.Description)).ToList();
            }

            if (toAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, toAdd);
                if (!addResult.Succeeded)
                    return addResult.Errors.Select(e => Error.Create(ErrorKind.Validation, e.Code, e.Description)).ToList();
            }

            return Result.Success();
        }

        public async Task<Result> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Error.NotFound(description: "User not found.");

            user.IsActive = false;
            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
                return res.Errors.Select(e => Error.Create(ErrorKind.Validation, e.Code, e.Description)).ToList();

            return Result.Success();
        }

        public async Task<Result> ReactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return Error.NotFound(description: "User not found.");

            user.IsActive = true;
            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
                return res.Errors.Select(e => Error.Create(ErrorKind.Validation, e.Code, e.Description)).ToList();

            return Result.Success();
        }

        public async Task<Result> VerifySpecialistAsync(Guid specialistId, bool isVerified, CancellationToken cancellationToken = default)
        {
            var specialist = await _context.Specialists.FirstOrDefaultAsync(s => s.Id == specialistId, cancellationToken);
            if (specialist == null)
                return Error.NotFound(description: "Specialist not found.");

            specialist.VerificationStatus = isVerified ? VerificationStatus.Approved : VerificationStatus.Rejected;
            specialist.VerifiedAt = isVerified ? DateTime.UtcNow : null;
            // VerifiedBy should be set by current user; leave null if not available.

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result<List<AuditLogDto>>> GetAuditLogsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var skip = (page - 1) * pageSize;
            var logs = await _context.Set<AuditLog>()
                .OrderByDescending(a => a.Timestamp)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = logs.Select(l => new AuditLogDto
            {
                Id = l.Id,
                Action = l.Action.ToString(),
                Details = l.NewValues ?? l.OldValues,
                PerformedBy = l.ChangedByUser?.Name,
                PerformedAt = l.Timestamp
            }).ToList();

            return Result<List<AuditLogDto>>.Success(dtos);
        }
    }
}
