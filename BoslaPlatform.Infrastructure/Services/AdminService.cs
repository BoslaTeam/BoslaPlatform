using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoslaPlatform.Application.Features.Admin.DTOs;
using BoslaPlatform.Application.Features.Admin.Services;
using BoslaPlatform.Application.Features.Lookup.Response;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Features.Admin.Repositories;
using BoslaPlatform.Application.Settings;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models;
using BoslaPlatform.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace BoslaPlatform.Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly StripeSettings _stripeSettings;
        private readonly IDashboardRepository _dashboardRepository;
        private readonly IUser _currentUser;
        public AdminService(IAppDbContext context, UserManager<User> userManager, IOptions<StripeSettings> stripeSettings, IDashboardRepository dashboardRepository, IUser currentUser)
        {
            _context = context;
            _userManager = userManager;
            _dashboardRepository = dashboardRepository;
            _currentUser = currentUser;
            _dashboardRepository = dashboardRepository;
            _stripeSettings = stripeOptions?.Value ?? new StripeSettings();
            if (!string.IsNullOrWhiteSpace(_stripeSettings.SecretKey))
            {
                StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
            }
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

        public async Task<Result<List<UserDto>>> ListAllUsersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var skip = (page - 1) * pageSize;
            var users = await _userManager.Users
                .IgnoreQueryFilters()
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

        public async Task<Result<List<SpecialistDto>>> GetPendingSpecialistsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var skip = (page - 1) * pageSize;
            var specialists = await _context.Specialists
                .IgnoreQueryFilters()
                .Include(s => s.User)
                .Where(s => s.VerificationStatus == VerificationStatus.Pending)
                .OrderBy(s => s.CreatedAtUtc)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = specialists.Select(s => new SpecialistDto
            {
                Id = s.Id,
                UserId = s.UserId,
                Name = s.User?.Name,
                Title = s.User?.Title,
                HourlyRate = s.HourlyRate,
                VerificationStatus = s.VerificationStatus.ToString()
            }).ToList();

            return Result<List<SpecialistDto>>.Success(dtos);
        }

        public async Task<Result<SpecialistDetailsDto>> GetSpecialistDetailAsync(Guid specialistId, CancellationToken cancellationToken = default)
        {
            var specialist = await _context.Specialists
                .IgnoreQueryFilters()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == specialistId, cancellationToken);

            if (specialist == null)
                return Error.NotFound(description: "Specialist not found.");

            var dto = new SpecialistDetailsDto
            {
                Id = specialist.Id,
                UserId = specialist.UserId,
                Name = specialist.User?.Name,
                Title = specialist.User?.Title,
                Bio = specialist.User?.Bio ?? specialist.BookingPolicy,
                HourlyRate = specialist.HourlyRate,
                ExperienceYears = specialist.ExperienceYears,
                VerificationStatus = specialist.VerificationStatus.ToString(),
                VerifiedAt = specialist.VerifiedAt
            };

            return Result<SpecialistDetailsDto>.Success(dto);
        }

        public async Task<Result<List<AppointmentDto>>> GetAllAppointmentsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var skip = (page - 1) * pageSize;
            var appts = await _context.Appointments
                .IgnoreQueryFilters()
                .OrderByDescending(a => a.CreatedAtUtc)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = appts.Select(a => new AppointmentDto
            {
                Id = a.Id,
                SpecialistId = a.SpecialistId,
                UserId = a.UserId,
                Start = a.Start.UtcDateTime,
                End = a.End.UtcDateTime,
                Status = a.Status.ToString(),
                Price = a.Payment?.Amount ?? 0m
            }).ToList();

            return Result<List<AppointmentDto>>.Success(dtos);
        }

        public async Task<Result> CancelTheAppointmentAsync(Guid appointmentId, string reason, CancellationToken cancellationToken = default)
        {
            var appt = await _context.Appointments.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);
            if (appt == null)
                return Error.NotFound(description: "Appointment not found.");

            var cancelResult = appt.Cancel(Guid.Empty, reason);
            if (!cancelResult.IsSuccess)
                return cancelResult.Errors;

            // persist and audit
            try
            {
                _context.Appointments.Update(appt);
                var audit = new BoslaPlatform.Domain.Models.AuditLog
                {
                    EntityType = "Appointment",
                    EntityId = appt.Id.ToString(),
                    Action = BoslaPlatform.Domain.Enums.AuditAction.Updated,
                    OldValues = null,
                    NewValues = $"Status={appt.Status};CancellationReason={appt.CancellationReason}",
                    Timestamp = DateTime.UtcNow
                };
                _context.Set<BoslaPlatform.Domain.Models.AuditLog>().Add(audit);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // swallow audit persistence failures
            }

            return Result.Success();
        }

        public async Task<Result> RescheduleAppointmentAsync(Guid appointmentId, DateTime newStart, DateTime newEnd, CancellationToken cancellationToken = default)
        {
            var appt = await _context.Appointments.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);
            if (appt == null)
                return Error.NotFound(description: "Appointment not found.");

            var newStartOffset = new DateTimeOffset(DateTime.SpecifyKind(newStart, DateTimeKind.Utc));
            var newEndOffset = new DateTimeOffset(DateTime.SpecifyKind(newEnd, DateTimeKind.Utc));

            // Overlap check
            var overlap = await _context.Appointments.IgnoreQueryFilters().AnyAsync(a => a.SpecialistId == appt.SpecialistId && a.Id != appt.Id &&
                (newStartOffset < a.End && newEndOffset > a.Start), cancellationToken);

            if (overlap)
                return Error.Validation("Appointment.Overlap", "The new time overlaps with an existing appointment for this specialist.");

            var res = appt.Reschedule(Guid.Empty, newStartOffset, newEndOffset, "Rescheduled by admin");
            if (!res.IsSuccess)
                return res.Errors;

            try
            {
                _context.Appointments.Update(appt);
                var audit = new BoslaPlatform.Domain.Models.AuditLog
                {
                    EntityType = "Appointment",
                    EntityId = appt.Id.ToString(),
                    Action = BoslaPlatform.Domain.Enums.AuditAction.Updated,
                    OldValues = null,
                    NewValues = $"Start={appt.Start};End={appt.End}",
                    Timestamp = DateTime.UtcNow
                };
                _context.Set<BoslaPlatform.Domain.Models.AuditLog>().Add(audit);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
            }

            return Result.Success();
        }

        public async Task<Result<List<PaymentDto>>> GetAllPaymentsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var skip = (page - 1) * pageSize;
            var payments = await _context.Payments
                .IgnoreQueryFilters()
                .OrderByDescending(p => p.PaidAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                AppointmentId = p.AppointmentId,
                UserId = p.Appointment != null ? p.Appointment.UserId : Guid.Empty,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status.ToString(),
                CreatedAt = (p.PaidAt ?? p.CreatedAtUtc).UtcDateTime
            }).ToList();

            return Result<List<PaymentDto>>.Success(dtos);
        }

        public async Task<Result> RefundPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
        {
            var payment = await _context.Payments.IgnoreQueryFilters().Include(p => p.Appointment).FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
            if (payment == null)
                return Error.NotFound(description: "Payment not found.");
            // Attempt external refund via Stripe if possible
            if (!string.IsNullOrWhiteSpace(payment.ExternalPaymentId) && !string.IsNullOrWhiteSpace(_stripeSettings.SecretKey))
            {
                try
                {
                    StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
                    var refundService = new RefundService();
                    var options = new RefundCreateOptions { PaymentIntent = payment.ExternalPaymentId };
                    var refund = await refundService.CreateAsync(options);

                    // mark payment as refunded
                    typeof(BoslaPlatform.Domain.Models.Booking.Payment).GetProperty(nameof(BoslaPlatform.Domain.Models.Booking.Payment.Status))?.SetValue(payment, Domain.Enums.PaymentStatus.Refunded);
                    typeof(BoslaPlatform.Domain.Models.Booking.Payment).GetProperty("RefundReason")?.SetValue(payment, $"Refunded via gateway: {refund.Id}");
                }
                catch (Exception ex)
                {
                    // fallback to marking as failed
                    payment.MarkAsFailed($"Refund failed: {ex.Message}");
                }
            }
            else
            {
                payment.MarkAsFailed("Refunded by admin");
            }

            try
            {
                _context.Payments.Update(payment);
                var audit = new BoslaPlatform.Domain.Models.AuditLog
                {
                    EntityType = "Payment",
                    EntityId = payment.Id.ToString(),
                    Action = BoslaPlatform.Domain.Enums.AuditAction.Updated,
                    OldValues = null,
                    NewValues = $"Status={payment.Status};RefundReason={payment.RefundReason}",
                    Timestamp = DateTime.UtcNow
                };
                _context.Set<BoslaPlatform.Domain.Models.AuditLog>().Add(audit);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
            }

            return Result.Success();
        }

        public async Task<Result<AuditLogDto>> GetAuditLogByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var l = await _context.Set<BoslaPlatform.Domain.Models.AuditLog>().IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (l == null)
                return Error.NotFound(description: "Audit log not found.");

            var dto = new AuditLogDto
            {
                Id = l.Id,
                Action = l.Action.ToString(),
                Details = l.NewValues ?? l.OldValues,
                PerformedBy = l.ChangedByUser?.Name,
                PerformedAt = l.Timestamp
            };

            return Result<AuditLogDto>.Success(dto);
        }

        public async Task<Result<DashboardDto>> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            // Prefer the Dapper read model for aggregated dashboard queries
            try
            {
                var dto = await _dashboardRepository.GetDashboardAsync(cancellationToken);
                return Result<DashboardDto>.Success(dto);
            }
            catch
            {
                // Fallback to EF counts if Dapper fails
                var totalUsers = await _context.Users.IgnoreQueryFilters().CountAsync(cancellationToken);
                var totalSpecialists = await _context.Specialists.IgnoreQueryFilters().CountAsync(cancellationToken);
                var pendingSpecialists = await _context.Specialists.IgnoreQueryFilters().CountAsync(s => s.VerificationStatus == VerificationStatus.Pending, cancellationToken: cancellationToken);
                var totalAppointments = await _context.Appointments.IgnoreQueryFilters().CountAsync(cancellationToken);
                var totalPayments = await _context.Payments.IgnoreQueryFilters().SumAsync(p => p.Amount, cancellationToken);

                var dto = new DashboardDto
                {
                    TotalUsers = totalUsers,
                    TotalSpecialists = totalSpecialists,
                    PendingSpecialists = pendingSpecialists,
                    TotalAppointments = totalAppointments,
                    TotalPayments = totalPayments
                };

                return Result<DashboardDto>.Success(dto);
            }
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

            // Write audit log
            try
            {
                var audit = new BoslaPlatform.Domain.Models.AuditLog
                {
                    EntityType = "User",
                    EntityId = user.Id.ToString(),
                    Action = BoslaPlatform.Domain.Enums.AuditAction.Updated,
                    OldValues = null,
                    NewValues = $"IsActive=false",
                    Timestamp = DateTime.UtcNow
                };
                _context.Set<BoslaPlatform.Domain.Models.AuditLog>().Add(audit);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // swallow audit failures to avoid breaking main flow
            }

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

            // Audit
            try
            {
                var audit = new BoslaPlatform.Domain.Models.AuditLog
                {
                    EntityType = "User",
                    EntityId = user.Id.ToString(),
                    Action = BoslaPlatform.Domain.Enums.AuditAction.Updated,
                    OldValues = null,
                    NewValues = $"IsActive=true",
                    Timestamp = DateTime.UtcNow
                };
                _context.Set<BoslaPlatform.Domain.Models.AuditLog>().Add(audit);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
            }

            return Result.Success();
        }

        public async Task<Result<PaginatedList<AdminSpecialistListItemDto>>> ListSpecialistsAsync(
            int page = 1,
            int pageSize = 20,
            string? search = null,
            string? verificationStatus = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Specialists
                .Include(s => s.User)
                .Include(s => s.SpecialistExpertise).ThenInclude(se => se.Expertise)
                .Include(s => s.Appointments)
                    .ThenInclude(a => a.Payment)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s =>
                    s.User.Name.Contains(search) ||
                    (s.User.Email != null && s.User.Email.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(verificationStatus)
                && Enum.TryParse<VerificationStatus>(verificationStatus, true, out var status))
            {
                query = query.Where(s => s.VerificationStatus == status);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var specialists = await query
                .OrderByDescending(s => s.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = specialists.Select(s => new AdminSpecialistListItemDto
            {
                Id = s.Id,
                FullName = s.User.Name,
                Email = s.User.Email ?? string.Empty,
                Title = s.User.Title,
                ProfileImageUrl = s.User.ProfileImageUrl,
                HourlyRate = s.HourlyRate,
                ExperienceLevel = s.ExperienceLevel.ToString(),
                VerificationStatus = s.VerificationStatus.ToString(),
                Rating = s.Reviews.Any() ? Math.Round(s.Reviews.Average(r => (double)r.Rating), 1) : 0,
                IsOnline = false,
                CreatedAt = s.CreatedAtUtc.UtcDateTime,
                ExpertiseAreas = s.SpecialistExpertise
                    .Select(se => se.Expertise.Name)
                    .ToList(),
                TotalSessions = s.Appointments.Count,
                TotalEarnings = s.Appointments
                    .Where(a => a.Payment != null && a.Payment.Status == PaymentStatus.Completed)
                    .Sum(a => a.Payment!.SpecialistAmount)
            }).ToList();

            var metadata = new PaginationMetadata(page, pageSize, totalCount);
            return Result<PaginatedList<AdminSpecialistListItemDto>>.Success(
                new PaginatedList<AdminSpecialistListItemDto>(dtos, metadata));
        }

        public async Task<Result<PaginatedList<AdminSpecialistListItemDto>>> ListPendingSpecialistsAsync(
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            return await ListSpecialistsAsync(page, pageSize, null, nameof(VerificationStatus.Pending), cancellationToken);
        }

        public async Task<Result<AdminSpecialistDetailDto>> GetSpecialistDetailAsync(
            Guid specialistId,
            CancellationToken cancellationToken = default)
        {
            var specialist = await _context.Specialists
                .Include(s => s.User)
                .Include(s => s.SpecialistExpertise).ThenInclude(se => se.Expertise)
                .Include(s => s.SpecialistIndustries).ThenInclude(si => si.Industry)
                .Include(s => s.SpecialistSkills).ThenInclude(ss => ss.Skill)
                .Include(s => s.SpecialistTools).ThenInclude(st => st.Tool)
                .Include(s => s.Experiences)
                .Include(s => s.Reviews).ThenInclude(r => r.Reviewer)
                .Include(s => s.Appointments).ThenInclude(a => a.Payment)
                .FirstOrDefaultAsync(s => s.Id == specialistId, cancellationToken);

            if (specialist == null)
                return Error.NotFound(description: "Specialist not found.");

            var dto = new AdminSpecialistDetailDto
            {
                Id = specialist.Id,
                UserId = specialist.UserId,
                FullName = specialist.User.Name,
                Email = specialist.User.Email ?? string.Empty,
                Title = specialist.User.Title,
                Bio = specialist.User.Bio,
                ProfileImageUrl = specialist.User.ProfileImageUrl,
                HourlyRate = specialist.HourlyRate,
                ExperienceLevel = specialist.ExperienceLevel.ToString(),
                ExperienceYears = specialist.ExperienceYears,
                Gender = specialist.User.Gender,
                Country = specialist.User.Country,
                PreferredLanguage = specialist.User.PreferredLanguage,
                VerificationStatus = specialist.VerificationStatus.ToString(),
                IsVerified = specialist.VerificationStatus == VerificationStatus.Approved,
                VerifiedAt = specialist.VerifiedAt,
                Rating = specialist.Reviews.Any()
                    ? Math.Round(specialist.Reviews.Average(r => (double)r.Rating), 1)
                    : 0,
                TotalReviews = specialist.Reviews.Count,
                TotalSessions = specialist.Appointments.Count,
                TotalEarnings = specialist.Appointments
                    .Where(a => a.Payment != null && a.Payment.Status == PaymentStatus.Completed)
                    .Sum(a => a.Payment!.SpecialistAmount),
                CreatedAt = specialist.CreatedAtUtc.UtcDateTime,
                LastLoginAt = specialist.User.LastLoginAt,
                ExpertiseAreas = specialist.SpecialistExpertise
                    .Select(se => se.Expertise.Name).ToList(),
                Industries = specialist.SpecialistIndustries
                    .Select(si => si.Industry.Name).ToList(),
                Skills = specialist.SpecialistSkills
                    .Select(ss => new SpecialistSkillItemDto
                    {
                        Id = ss.SkillId,
                        Name = ss.Skill.Name
                    }).ToList(),
                Tools = specialist.SpecialistTools
                    .Select(st => new SpecialistToolItemDto
                    {
                        Id = st.ToolId,
                        Name = st.Tool.Name
                    }).ToList(),
                Experiences = specialist.Experiences
                    .Select(e => new SpecialistExperienceItemDto
                    {
                        Id = e.Id,
                        JobTitle = e.JobTitle,
                        CompanyName = e.CompanyName,
                        FromDate = e.FromDate,
                        ToDate = e.ToDate,
                        Description = e.Description
                    }).ToList(),
                Reviews = specialist.Reviews
                    .Select(r => new SpecialistReviewItemDto
                    {
                        Id = r.Id,
                        ReviewerName = r.Reviewer.Name,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAtUtc.UtcDateTime
                    }).ToList()
            };

            return Result<AdminSpecialistDetailDto>.Success(dto);
        }

        public async Task<Result> VerifyOfSpecialistAsync(Guid specialistId, bool isVerified, Guid verifiedByUserId, CancellationToken cancellationToken = default)
        {
            var specialist = await _context.Specialists.FirstOrDefaultAsync(s => s.Id == specialistId, cancellationToken);
            if (specialist == null)
                return Error.NotFound(description: "Specialist not found.");

            var old = specialist.VerificationStatus;

            specialist.VerificationStatus = isVerified ? VerificationStatus.Approved : VerificationStatus.Rejected;
            specialist.VerifiedAt = isVerified ? DateTime.UtcNow : null;
            await _context.SaveChangesAsync(cancellationToken);

            // Audit
            try
            {
                var audit = new BoslaPlatform.Domain.Models.AuditLog
                {
                    EntityType = "Specialist",
                    EntityId = specialist.Id.ToString(),
                    Action = BoslaPlatform.Domain.Enums.AuditAction.Verified,
                    OldValues = $"VerificationStatus={old}",
                    NewValues = $"VerificationStatus={specialist.VerificationStatus}",
                    Timestamp = DateTime.UtcNow
                };
                _context.Set<BoslaPlatform.Domain.Models.AuditLog>().Add(audit);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
            }

            return Result.Success();
        }

        // VerifiedBy should be set by current user; leave null if not available.

        public async Task<Result> UpdateToolAsync(Guid id, string name, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Tools.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null) return Error.NotFound(description: "Tool not found.");
            entity.Name = name;
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> DeleteToolAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Tools.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null) return Error.NotFound(description: "Tool not found.");
            _context.Tools.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result<List<AuditLogDto>>> GetAuditLogsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var skip = (page - 1) * pageSize;
            var logs = await _context.Set<BoslaPlatform.Domain.Models.AuditLog>()
                .IgnoreQueryFilters()
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

        public async Task<Result> VerifyTheSpecialistAsync(Guid specialistId, bool isVerified, CancellationToken cancellationToken = default)
        {
            var specialist = await _context.Specialists.FirstOrDefaultAsync(s => s.Id == specialistId, cancellationToken);
            if (specialist == null)
                return Error.NotFound(description: "Specialist not found.");

            var old = specialist.VerificationStatus;

            specialist.VerificationStatus = isVerified ? VerificationStatus.Approved : VerificationStatus.Rejected;
            specialist.VerifiedAt = isVerified ? DateTime.UtcNow : null;

            await _context.SaveChangesAsync(cancellationToken);

            // Audit
            try
            {
                var audit = new BoslaPlatform.Domain.Models.AuditLog
                {
                    EntityType = "Specialist",
                    EntityId = specialist.Id.ToString(),
                    Action = BoslaPlatform.Domain.Enums.AuditAction.Verified,
                    OldValues = $"VerificationStatus={old}",
                    NewValues = $"VerificationStatus={specialist.VerificationStatus}",
                    Timestamp = DateTime.UtcNow
                };
                _context.Set<BoslaPlatform.Domain.Models.AuditLog>().Add(audit);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
            }

            return Result.Success();
        }


        public async Task<Result> VerifySpecialistAsync(Guid specialistId, bool isVerified, Guid verifiedByUserId, CancellationToken cancellationToken = default)
        {
            var specialist = await _context.Specialists.FirstOrDefaultAsync(s => s.Id == specialistId, cancellationToken);
            if (specialist == null)
                return Error.NotFound(description: "Specialist not found.");

            specialist.VerificationStatus = isVerified ? VerificationStatus.Approved : VerificationStatus.Rejected;
            specialist.VerifiedAt = isVerified ? DateTime.UtcNow : null;
            specialist.VerifiedBy = isVerified ? verifiedByUserId : null;

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        // ── Appointments ──

        public async Task<Result<PaginatedList<AdminAppointmentDto>>> ListAppointmentsAsync(
            int page = 1,
            int pageSize = 20,
            string? search = null,
            int? status = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Specialist).ThenInclude(s => s.User)
                .Include(a => a.Payment)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a =>
                    a.User.Name.Contains(search) ||
                    (a.User.Email != null && a.User.Email.Contains(search)) ||
                    a.Specialist.User.Name.Contains(search));
            }

            if (status.HasValue && Enum.IsDefined(typeof(AppointmentStatus), status.Value))
            {
                var statusEnum = (AppointmentStatus)status.Value;
                query = query.Where(a => a.Status == statusEnum);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var appointments = await query
                .OrderByDescending(a => a.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = appointments.Select(a => new AdminAppointmentDto
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

            var metadata = new PaginationMetadata(page, pageSize, totalCount);
            return Result<PaginatedList<AdminAppointmentDto>>.Success(
                new PaginatedList<AdminAppointmentDto>(dtos, metadata));
        }

        public async Task<Result<AdminAppointmentDetailDto>> GetAppointmentDetailAsync(
            Guid appointmentId,
            CancellationToken cancellationToken = default)
        {
            var appointment = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Specialist).ThenInclude(s => s.User)
                .Include(a => a.Payment)
                .Include(a => a.StatusHistory)
                .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

            if (appointment == null)
                return Error.NotFound(description: "Appointment not found.");

            var dto = new AdminAppointmentDetailDto
            {
                Id = appointment.Id,
                UserId = appointment.UserId,
                UserName = appointment.User?.Name ?? string.Empty,
                UserEmail = appointment.User?.Email,
                UserAvatarUrl = appointment.User?.ProfileImageUrl,
                SpecialistId = appointment.SpecialistId,
                SpecialistName = appointment.Specialist?.User?.Name ?? string.Empty,
                SpecialistAvatarUrl = appointment.Specialist?.User?.ProfileImageUrl,
                Start = appointment.Start,
                End = appointment.End,
                DurationMinutes = (int)(appointment.End - appointment.Start).TotalMinutes,
                Status = appointment.Status.ToString(),
                SessionTopic = appointment.SessionTopic,
                Notes = appointment.Notes,
                CancellationReason = appointment.CancellationReason,
                TotalAmount = appointment.Payment?.Amount,
                PaymentStatus = appointment.Payment?.Status.ToString(),
                CreatedAt = appointment.CreatedAtUtc.UtcDateTime,
                StatusHistory = appointment.StatusHistory.Select(h => new AdminAppointmentStatusHistoryDto
                {
                    OldStatus = h.OldStatus.ToString(),
                    NewStatus = h.NewStatus.ToString(),
                    Reason = h.Reason,
                    CreatedAt = h.CreatedAtUtc.UtcDateTime
                }).ToList()
            };

            return Result<AdminAppointmentDetailDto>.Success(dto);
        }

        public async Task<Result> CancelAppointmentAsync(Guid appointmentId, string reason, CancellationToken cancellationToken = default)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

            if (appointment == null)
                return Error.NotFound(description: "Appointment not found.");

            var userId = _currentUser.Id;
            if (userId == null)
                return Error.Unauthorized(description: "Admin user not found.");

            var cancelResult = appointment.Cancel(userId.Value, reason);
            if (!cancelResult.IsSuccess)
                return cancelResult.Errors;

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> ConfirmAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

            if (appointment == null)
                return Error.NotFound(description: "Appointment not found.");

            var userId = _currentUser.Id;
            if (userId == null)
                return Error.Unauthorized(description: "Admin user not found.");

            var result = appointment.Confirm(appointment.SpecialistId);
            if (!result.IsSuccess)
                return result.Errors;

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> CompleteAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

            if (appointment == null)
                return Error.NotFound(description: "Appointment not found.");

            var specialistId = appointment.SpecialistId;
            var result = appointment.Complete(specialistId);
            if (!result.IsSuccess)
                return result.Errors;

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> UpdateSpecialistStatusAsync(Guid specialistId, string status, Guid? verifiedByUserId, CancellationToken cancellationToken = default)
        {
            var specialist = await _context.Specialists.FirstOrDefaultAsync(s => s.Id == specialistId, cancellationToken);
            if (specialist == null)
                return Error.NotFound(description: "Specialist not found.");

            if (!Enum.TryParse<VerificationStatus>(status, true, out var parsedStatus))
                return Error.Validation(description: $"Invalid verification status: {status}");

            specialist.VerificationStatus = parsedStatus;
            specialist.VerifiedAt = parsedStatus == VerificationStatus.Approved ? DateTime.UtcNow : null;
            specialist.VerifiedBy = parsedStatus == VerificationStatus.Approved ? verifiedByUserId : null;

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        // ── Expertise ──

        public async Task<Result<List<LookupItemResponse>>> GetExpertiseListAsync(CancellationToken cancellationToken = default)
        {
            var items = await _context.Expertises
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemResponse(x.Id, x.Name))
                .ToListAsync(cancellationToken);
            return items;
        }

        public async Task<Result<Guid>> CreateExpertiseAsync(string name, CancellationToken cancellationToken = default)
        {
            var entity = new BoslaPlatform.Domain.Models.Lookup.Expertise { Name = name };
            _context.Expertises.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }

        public async Task<Result> UpdateExpertiseAsync(Guid id, string name, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Expertises.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null) return Error.NotFound(description: "Expertise not found.");
            entity.Name = name;
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> DeleteExpertiseAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Expertises.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null) return Error.NotFound(description: "Expertise not found.");
            _context.Expertises.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        // ── Skills ──

        public async Task<Result<List<LookupItemResponse>>> GetSkillListAsync(CancellationToken cancellationToken = default)
        {
            var items = await _context.Skills
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemResponse(x.Id, x.Name))
                .ToListAsync(cancellationToken);
            return items;
        }

        public async Task<Result<Guid>> CreateSkillAsync(string name, CancellationToken cancellationToken = default)
        {
            var entity = new BoslaPlatform.Domain.Models.Lookup.Skill { Name = name };
            _context.Skills.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }

        public async Task<Result> UpdateSkillAsync(Guid id, string name, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Skills.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null) return Error.NotFound(description: "Skill not found.");
            entity.Name = name;
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> DeleteSkillAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Skills.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null) return Error.NotFound(description: "Skill not found.");
            _context.Skills.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        // ── Tools ──

        public async Task<Result<List<LookupItemResponse>>> GetToolListAsync(CancellationToken cancellationToken = default)
        {
            var items = await _context.Tools
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new LookupItemResponse(x.Id, x.Name))
                .ToListAsync(cancellationToken);
            return items;
        }

        public async Task<Result<Guid>> CreateToolAsync(string name, CancellationToken cancellationToken = default)
        {
            var entity = new BoslaPlatform.Domain.Models.Lookup.Tool { Name = name };
            _context.Tools.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return entity.Id;
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
