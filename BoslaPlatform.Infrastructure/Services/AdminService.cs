using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoslaPlatform.Application.Features.Admin.DTOs;
using BoslaPlatform.Application.Features.Admin.Services;
using BoslaPlatform.Application.Interfaces.Persistence;
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
        private readonly BoslaPlatform.Application.Features.Admin.Repositories.IDashboardRepository _dashboardRepository;
        public AdminService(IAppDbContext context, UserManager<User> userManager, IOptions<StripeSettings> stripeOptions, BoslaPlatform.Application.Features.Admin.Repositories.IDashboardRepository dashboardRepository)
        {
            _context = context;
            _userManager = userManager;
            _dashboardRepository = dashboardRepository;
            _stripeSettings = stripeOptions?.Value ?? new StripeSettings();
            if (!string.IsNullOrWhiteSpace(_stripeSettings.SecretKey))
            {
                StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
            }
        }

        public async Task<Result<List<UserDto>>> ListUsersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
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
            var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
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

        public async Task<Result> CancelAppointmentAsync(Guid appointmentId, string reason, CancellationToken cancellationToken = default)
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

        public async Task<Result> VerifySpecialistAsync(Guid specialistId, bool isVerified, CancellationToken cancellationToken = default)
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
    }
}
