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

        public async Task<Result<BoslaPlatform.Shared.PaginatedList<UserDto>>> ListUsersAsync(int page = 1, int pageSize = 20, string? search = null, int? role = null, bool? isActive = null, CancellationToken cancellationToken = default)
        {
            var query = _userManager.Users.IgnoreQueryFilters().AsQueryable();

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.Email!.Contains(search) || u.Name.Contains(search));

            if (role.HasValue)
            {
                string roleName = role.Value == 2 ? "Admin" : (role.Value == 1 ? "Specialist" : "User");
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
                var userIds = usersInRole.Select(u => u.Id).ToList();
                query = query.Where(u => userIds.Contains(u.Id));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderByDescending(u => u.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
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
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAtUtc.UtcDateTime,
                    AvatarUrl = u.ProfileImageUrl
                };

                var roles = await _userManager.GetRolesAsync(u);
                dto.Roles = roles.ToArray();
                dto.Role = roles.Contains("Admin") ? 2 : (roles.Contains("Specialist") ? 1 : 0);
                dtos.Add(dto);
            }

            var metadata = new BoslaPlatform.Shared.PaginationMetadata(page, pageSize, totalCount);
            return Result<BoslaPlatform.Shared.PaginatedList<UserDto>>.Success(new BoslaPlatform.Shared.PaginatedList<UserDto>(dtos, metadata));
        }

        public async Task<Result<UserDetailsDto>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users
                .IgnoreQueryFilters()
                .Include(u => u.Educations)
                .Include(u => u.SocialLinks)
                .Include(u => u.Appointments)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
                
            if (user == null)
                return Error.NotFound(description: "User not found.");

            var dto = new UserDetailsDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.Name,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAtUtc.UtcDateTime,
                PhoneNumber = user.PhoneNumber,
                Country = user.Country,
                Title = user.Title,
                Bio = user.Bio,
                Gender = user.Gender,
                PreferredLanguage = user.PreferredLanguage,
                ProfilePictureUrl = user.ProfileImageUrl,
                AvatarUrl = user.ProfileImageUrl,
                LastLoginAt = user.LastLoginAt,
                AppointmentsCount = user.Appointments?.Count ?? 0,
                Education = user.Educations?.Select(e => new EducationItemDto
                {
                    Id = e.Id,
                    Degree = e.FieldOfStudy,
                    Institution = e.InstitutionName,
                    StartYear = e.StartDate.Year,
                    EndYear = e.EndDate?.Year
                }).ToList() ?? new List<EducationItemDto>(),
                SocialLinks = user.SocialLinks?.Select(s => new SocialLinkItemDto
                {
                    Id = s.Id,
                    Platform = s.Title,
                    Url = s.Url
                }).ToList() ?? new List<SocialLinkItemDto>()
            };
            
            var roles = await _userManager.GetRolesAsync(user);
            dto.Roles = roles.ToArray();
            dto.Role = roles.Contains("Admin") ? 2 : (roles.Contains("Specialist") ? 1 : 0);
            return Result<UserDetailsDto>.Success(dto);
        }

        public async Task<Result> CreateUserAsync(BoslaPlatform.Application.Features.Admin.Requests.CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                Name = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Country = request.Country,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
                return createResult.Errors.Select(e => Error.Create(ErrorKind.Validation, e.Code, e.Description)).ToList();

            string roleName = request.Role == 2 ? "Admin" : (request.Role == 1 ? "Specialist" : "User");
            await _userManager.AddToRoleAsync(user, roleName);

            return Result.Success();
        }

        public async Task<Result> UpdateUserAsync(Guid userId, BoslaPlatform.Application.Features.Admin.Requests.UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
                return Error.NotFound(description: "User not found.");

            user.Name = request.FullName ?? user.Name;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
            user.Country = request.Country ?? user.Country;
            user.Title = request.Title ?? user.Title;
            user.Bio = request.Bio ?? user.Bio;
            user.Gender = request.Gender ?? user.Gender;
            user.PreferredLanguage = request.PreferredLanguage ?? user.PreferredLanguage;
            user.IsActive = request.IsActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return updateResult.Errors.Select(e => Error.Create(ErrorKind.Validation, e.Code, e.Description)).ToList();

            string roleName = request.Role == 2 ? "Admin" : (request.Role == 1 ? "Specialist" : "User");
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(roleName))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, roleName);
            }

            return Result.Success();
        }

        public async Task<Result> UpdateUserRolesAsync(Guid userId, List<string> roles, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
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
            var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
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
            var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
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
            var logs = await _context.Set<BoslaPlatform.Domain.Models.AuditLog>()
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
        public async Task<Result<AdminDashboardDto>> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
        {
            var totalUsers = await _userManager.Users.CountAsync(cancellationToken);
            var totalSpecialists = await _context.Specialists.CountAsync(cancellationToken);
            var totalAppointments = await _context.Appointments.CountAsync(cancellationToken);
            
            var totalRevenue = await _context.Payments
                .Where(p => p.Status == BoslaPlatform.Domain.Enums.PaymentStatus.Completed)
                .SumAsync(p => p.Amount, cancellationToken);

            var pendingVerifications = await _context.Specialists
                .CountAsync(s => s.VerificationStatus == BoslaPlatform.Domain.Enums.VerificationStatus.Pending, cancellationToken);

            var activeAppointments = await _context.Appointments
                .CountAsync(a => a.Status == BoslaPlatform.Domain.Enums.AppointmentStatus.Confirmed || a.Status == BoslaPlatform.Domain.Enums.AppointmentStatus.Rescheduled, cancellationToken);

            var recentUsersList = await _userManager.Users
                .OrderByDescending(u => u.CreatedAtUtc)
                .Take(5)
                .ToListAsync(cancellationToken);

            var recentUsersDtos = new List<UserDto>();
            foreach (var u in recentUsersList)
            {
                var dto = new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.Name,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAtUtc.UtcDateTime
                };
                var roles = await _userManager.GetRolesAsync(u);
                dto.Roles = roles.ToArray();
                dto.Role = roles.Contains("Admin") ? 2 : (roles.Contains("Specialist") ? 1 : 0);
                recentUsersDtos.Add(dto);
            }

            var recentAppointmentsList = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Specialist).ThenInclude(s => s.User)
                .Include(a => a.Payment)
                .OrderByDescending(a => a.CreatedAtUtc)
                .Take(5)
                .ToListAsync(cancellationToken);

            var recentAppointmentDtos = recentAppointmentsList.Select(a => new AdminAppointmentDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User?.Name ?? string.Empty,
                SpecialistId = a.SpecialistId,
                SpecialistName = a.Specialist?.User?.Name ?? string.Empty,
                ScheduledAt = a.Start.UtcDateTime,
                DurationMinutes = (int)(a.End - a.Start).TotalMinutes,
                Status = (int)a.Status,
                TotalAmount = a.Payment?.Amount ?? 0,
                CreatedAt = a.CreatedAtUtc.UtcDateTime
            }).ToList();

            var dtoResult = new AdminDashboardDto
            {
                TotalUsers = totalUsers,
                TotalSpecialists = totalSpecialists,
                TotalAppointments = totalAppointments,
                TotalRevenue = totalRevenue,
                PendingVerifications = pendingVerifications,
                ActiveAppointments = activeAppointments,
                RecentUsers = recentUsersDtos,
                RecentAppointments = recentAppointmentDtos,
                // Hardcoding growth percentages as 0 since they are not strictly queried in this ticket
                UserGrowthPercentage = 0,
                RevenueGrowthPercentage = 0,
                AppointmentGrowthPercentage = 0,
                SpecialistGrowthPercentage = 0
            };

            return Result<AdminDashboardDto>.Success(dtoResult);
        }
    }
}
