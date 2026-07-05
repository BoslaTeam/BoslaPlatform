using System.Collections;
using BoslaPlatform.Application.Features.Appointments.DTOs;
using BoslaPlatform.Application.Features.Appointments.Requests;
using BoslaPlatform.Application.Features.Appointments.Services;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Entities;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Apoointments;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppDbContext _context;
        private readonly IUser _currentUser;

        public AppointmentService(IAppDbContext context, IUser currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        // 1. Create Appointment
        public async Task<Result<Guid>> CreateAsync(CreateAppointmentRequest request, CancellationToken ct)
        {
            if (!_currentUser.IsAuthenticated || !_currentUser.Id.HasValue)
            {
                return Error.Unauthorized("Auth.Unauthorized", "User must be logged in to schedule an appointment.");
            }
           
            var specialistExists = await _context.Specialists.AnyAsync(s => s.Id == request.SpecialistId, ct);
            if (!specialistExists)
            {
                return Error.NotFound("Specialist.NotFound", "The requested specialist does not exist.");
            }
            if (request.Start >= request.End)
            {
                return Error.Validation("Appointment.InvalidTimeRange", "The appointment start time must be before the end time.");
            }

            // Overlap Check
            bool hasOverlap = await _context.Appointments
                .AnyAsync(a =>
                    a.SpecialistId == request.SpecialistId &&
                    a.Status != AppointmentStatus.Cancelled &&
                    request.Start < a.End &&
                    request.End > a.Start,
                    ct);

            if (hasOverlap)
            {
                return Error.Conflict("Appointment.Overlap", "The specialist is already booked during this time interval.");
            }

            Guid currentUserId = _currentUser.Id.Value;

            var specialist = await _context.Specialists
                .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Id == request.SpecialistId,
                ct);

            if (specialist is null)
            {
                return Error.NotFound(
                    "Specialist.NotFound",
                    "The requested specialist does not exist.");
            }

            var durationHours = (decimal)(request.End - request.Start).TotalHours;
            var sessionPrice = specialist.HourlyRate * durationHours;

            var appointment = Appointment.Schedule(
                   request.SpecialistId,
                    currentUserId,
                    request.Start,
                    request.End,
                    request.SessionTopic,
                    request.Notes,
                    sessionPrice

            );

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync(ct);

            appointment.AddDomainEvent(new AppointmentScheduledEvent(appointment.Id, appointment.SpecialistId, appointment.UserId, appointment.Start));
            return appointment.Id;
        }

        // 2. Read Single Appointment
        public async Task<Result<AppointmentDto>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var appointment = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.User)
                .Include(a => a.Specialist)
                    .ThenInclude(s => s.User)
                .Where(a => a.Id == id)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    SpecialistId = a.SpecialistId,
                    SpecialistName = a.Specialist.User.Name,
                    UserId = a.UserId,
                    UserName = a.User.Name,
                    Start = a.Start,
                    End = a.End,
                    Status = a.Status,
                    SessionTopic = a.SessionTopic,
                    Notes = a.Notes,
                    SessionPrice = a.SessionPrice,
                    ConfirmedAt = a.ConfirmedAt,

                    IsPaid = a.Payment != null
                        && a.Payment.Status == PaymentStatus.Completed,
                    ConversationId = a.Conversation != null ? a.Conversation.Id : (Guid?)null
                })
                .FirstOrDefaultAsync(ct);

            if (appointment is null)
            {
                return Error.NotFound("Appointment.NotFound", "The requested appointment was not found.");
            }

            return appointment;
        }

        // 3. Read Paginated List for Logged-In User (Patient + Specialist)
        public async Task<Result<PaginatedList<AppointmentDto>>> GetMyAppointmentsAsync(int pageNumber, int pageSize, CancellationToken ct)
        {
            if (!_currentUser.Id.HasValue)
            {
                return Error.Unauthorized("Auth.Unauthorized", "User identification missing.");
            }

            Guid currentUserId = _currentUser.Id.Value;

            // Resolve Specialist.Id if the current user has a specialist profile
            var specialistId = await _context.Specialists
                .Where(s => s.UserId == currentUserId)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);

            var query = _context.Appointments
                .AsNoTracking()
                .Where(a => a.UserId == currentUserId);

            if (specialistId.HasValue)
            {
                query = _context.Appointments
                    .AsNoTracking()
                    .Where(a => a.UserId == currentUserId || a.SpecialistId == specialistId.Value);
            }

            int totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(a => a.Start)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    SpecialistId = a.SpecialistId,
                    UserId = a.UserId,
                    Start = a.Start,
                    End = a.End,
                    Status = a.Status,
                    SessionTopic = a.SessionTopic,
                    Notes = a.Notes,
                    SessionPrice = a.SessionPrice,
                    IsPaid = a.Payment != null && a.Payment.Status == PaymentStatus.Completed,
                    ConversationId = a.Conversation != null ? a.Conversation.Id : (Guid?)null,
                    ConfirmedAt = a.ConfirmedAt
                })
                .ToListAsync(ct);

            var metadata = PaginationMetadata.Create(pageNumber, pageSize, totalCount);

            return new PaginatedList<AppointmentDto>(items, metadata);
        }

        // 4. Read Paginated List for Specific Specialist
        public async Task<Result<PaginatedList<AppointmentDto>>> GetSpecialistAppointmentsAsync(Guid specialistId, int pageNumber, int pageSize, CancellationToken ct)
        {
            var query = _context.Appointments
                .AsNoTracking()
                .Where(a => a.SpecialistId == specialistId);

            int totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(a => a.Start)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    SpecialistId = a.SpecialistId,
                    UserId = a.UserId,
                    Start = a.Start,
                    End = a.End,
                    Status = a.Status,
                    SessionTopic = a.SessionTopic,
                    Notes = a.Notes,
                    SessionPrice = a.SessionPrice,
                    IsPaid = a.Payment != null && a.Payment.Status == PaymentStatus.Completed,
                    ConversationId = a.Conversation != null ? a.Conversation.Id : (Guid?)null,
                    ConfirmedAt = a.ConfirmedAt
                })
                .ToListAsync(ct);

            var metadata = PaginationMetadata.Create(pageNumber, pageSize, totalCount);

            return new PaginatedList<AppointmentDto>(items, metadata);
        }

        // 4b. Read Paginated List for Current Specialist
        public async Task<Result<PaginatedList<AppointmentDto>>> GetMySpecialistAppointmentsAsync(int pageNumber, int pageSize, CancellationToken ct)
        {
            if (!_currentUser.Id.HasValue)
            {
                return Error.Unauthorized("Auth.Unauthorized", "User identification missing.");
            }

            var specialistId = await _context.Specialists
                .Where(s => s.UserId == _currentUser.Id.Value)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);

            if (!specialistId.HasValue)
            {
                return Error.Forbidden("Appointment.NotSpecialist", "Current user does not have a specialist profile.");
            }

            var query = _context.Appointments
                .AsNoTracking()
                .Where(a => a.SpecialistId == specialistId.Value);

            int totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(a => a.Start)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    SpecialistId = a.SpecialistId,
                    UserId = a.UserId,
                    Start = a.Start,
                    End = a.End,
                    Status = a.Status,
                    SessionTopic = a.SessionTopic,
                    Notes = a.Notes,
                    SessionPrice = a.SessionPrice,
                    IsPaid = a.Payment != null && a.Payment.Status == PaymentStatus.Completed,
                    ConversationId = a.Conversation != null ? a.Conversation.Id : (Guid?)null,
                    ConfirmedAt = a.ConfirmedAt
                })
                .ToListAsync(ct);

            var metadata = PaginationMetadata.Create(pageNumber, pageSize, totalCount);
            return new PaginatedList<AppointmentDto>(items, metadata);
        }

        // 5. Get Upcoming Appointments
        public async Task<Result<List<AppointmentDto>>> GetUpcomingAppointmentsAsync(CancellationToken ct)
        {
            if (!_currentUser.Id.HasValue)
            {
                return Error.Unauthorized("Auth.Unauthorized", "User identification missing.");
            }

            Guid currentUserId = _currentUser.Id.Value;

            // Resolve Specialist.Id if the current user has a specialist profile
            var specialistId = await _context.Specialists
                .Where(s => s.UserId == currentUserId)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);

            // Fetch active upcoming appointments for either the patient or specialist
            var upcomingAppointments = await _context.Appointments
                .AsNoTracking()
                .Where(a => (a.UserId == currentUserId ||
                             (specialistId.HasValue && a.SpecialistId == specialistId.Value)) &&
                            a.Start >= DateTimeOffset.UtcNow &&
                            a.Status != AppointmentStatus.Cancelled)
                .OrderBy(a => a.Start)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    SpecialistId = a.SpecialistId,
                    UserId = a.UserId,
                    Start = a.Start,
                    End = a.End,
                    Status = a.Status,
                    SessionTopic = a.SessionTopic,
                    Notes = a.Notes,
                    SessionPrice = a.SessionPrice,
                    IsPaid = a.Payment != null && a.Payment.Status == PaymentStatus.Completed,
                    ConversationId = a.Conversation != null ? a.Conversation.Id : (Guid?)null,
                    ConfirmedAt = a.ConfirmedAt
                })
                .ToListAsync(ct);

            return upcomingAppointments;
        }

        public async Task<Result<List<AppointmentStatusHistoryDto>>> GetStatusHistoryAsync(Guid id, CancellationToken ct)
        {
            var appointment = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.StatusHistory)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (appointment is null)
            {
                return Error.NotFound("Appointment.NotFound", "The appointment was not found.");
            }

            var historyDto = appointment.StatusHistory
                .Select(h => new AppointmentStatusHistoryDto
                {
                    Id = h.Id,
                    OldStatus = h.OldStatus,
                    NewStatus = h.NewStatus,
                    ChangedAt = h.CreatedAtUtc,
                    ChangedBy = h.CreatedBy ?? Guid.Empty,      
                    Reason = h.Reason
                })
                .ToList();

            return historyDto;
        }

        // 6a. Confirm Payment after Stripe success
        public async Task<Result> ConfirmPaymentAsync(Guid id, string paymentIntentId, CancellationToken ct)
        {
            var appointment = await _context.Appointments
                .Include(a => a.StatusHistory)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (appointment is null) return Error.NotFound("Appointment.NotFound", "The appointment was not found.");

            if (!_currentUser.Id.HasValue || appointment.UserId != _currentUser.Id.Value)
                return Error.Forbidden("Payment.Forbidden", "You are not authorized to confirm payment for this appointment.");

            if (appointment.ConfirmedAt.HasValue && appointment.ConfirmedAt.Value.AddHours(1) < DateTimeOffset.UtcNow)
                return Error.Validation("Payment.DeadlineExpired", "The payment deadline has passed. The appointment has been cancelled.");

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.AppointmentId == id, ct);

            if (payment is null) return Error.NotFound("Payment.NotFound", "No payment found for this appointment.");

            payment.Complete(paymentIntentId, "Card");

            var result = appointment.MarkAsPaid();
            if (result.IsError) return result.Errors;

            await _context.SaveChangesAsync(ct);
            return Result.Success();
        }

        // 7. Confirm Appointment
        public async Task<Result> ConfirmAsync(Guid id, CancellationToken ct)
        {
            var appointment = await _context.Appointments
                .Include(a => a.StatusHistory)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (appointment is null) return Error.NotFound("Appointment.NotFound", "The appointment was not found.");

            if (!_currentUser.Id.HasValue) return Error.Unauthorized("Auth.Unauthorized", "Specialist identification missing.");

            Guid currentSpecialistId = _currentUser.Id.Value;

            var result = appointment.Confirm(currentSpecialistId);
            if (result.IsError) return result.Errors;

            await _context.SaveChangesAsync(ct);
            return Result.Success();
        }

        // 8. Cancel Appointment
        public async Task<Result> CancelAsync(Guid id, string? reason, CancellationToken ct)
        {
            var appointment = await _context.Appointments
                .Include(a => a.StatusHistory)
                .Include(a => a.Payment)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (appointment is null) return Error.NotFound("Appointment.NotFound", "The appointment was not found.");

            if (!_currentUser.Id.HasValue) return Error.Unauthorized("Auth.Unauthorized", "User identification missing.");

            Guid executingUserId = _currentUser.Id.Value;

            var result = appointment.Cancel(executingUserId, reason ?? "");
            if (result.IsError) return result.Errors;

            if (appointment.Payment is not null && appointment.Payment.Status == PaymentStatus.Completed)
            {
                appointment.Payment.MarkAsRefunded("Appointment cancelled.");
            }

            await _context.SaveChangesAsync(ct);
            return Result.Success();
        }

        // 9. Reschedule Appointment
        public async Task<Result> RescheduleAsync(Guid id, DateTimeOffset newStart, DateTimeOffset newEnd, string reason, CancellationToken ct)
        {
            var appointment = await _context.Appointments
                .Include(a => a.StatusHistory)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (appointment is null) return Error.NotFound("Appointment.NotFound", "The appointment was not found.");

            // Overlap Check for the new requested slot
            bool hasOverlap = await _context.Appointments
                .AnyAsync(a =>
                    a.Id != id &&
                    a.SpecialistId == appointment.SpecialistId &&
                    a.Status != AppointmentStatus.Cancelled &&
                    newStart < a.End &&
                    newEnd > a.Start,
                    ct);

            if (hasOverlap) return Error.Conflict("Appointment.Overlap", "The specialist has another appointment booked during this newly requested slot.");

            if (!_currentUser.Id.HasValue) return Error.Unauthorized("Auth.Unauthorized", "User identification missing.");

            Guid executingUserId = _currentUser.Id.Value;

            var result = appointment.Reschedule(executingUserId, newStart, newEnd, reason);
            if (result.IsError) return result.Errors;

            await _context.SaveChangesAsync(ct);
            return Result.Success();
        }

        // 10. Complete Appointment
        public async Task<Result> CompleteAsync(Guid id, CancellationToken ct)
        {
            var appointment = await _context.Appointments
                .Include(a => a.StatusHistory)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (appointment is null) return Error.NotFound("Appointment.NotFound", "The appointment was not found.");

            if (!_currentUser.Id.HasValue) return Error.Unauthorized("Auth.Unauthorized", "Specialist identification missing.");

            Guid currentSpecialistId = _currentUser.Id.Value;

            var result = appointment.Complete(currentSpecialistId);
            if (result.IsError) return result.Errors;

            await _context.SaveChangesAsync(ct);
            return Result.Success();
        }

        // 11. Reject Appointment Request
        public async Task<Result> RejectAsync(Guid id, string reason, CancellationToken ct)
        {
            var appointment = await _context.Appointments
                .Include(a => a.StatusHistory)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (appointment is null) return Error.NotFound("Appointment.NotFound", "The appointment was not found.");

            if (!_currentUser.Id.HasValue) return Error.Unauthorized("Auth.Unauthorized", "Specialist identification missing.");

            Guid currentSpecialistId = _currentUser.Id.Value;

            var result = appointment.Cancel(currentSpecialistId, $"Rejected: {reason}");
            if (result.IsError) return result.Errors;

            await _context.SaveChangesAsync(ct);
            return Result.Success();
        }

        // 12. Update Appointment Notes
        public async Task<Result> UpdateNotesAsync(Guid id, string notes, CancellationToken ct)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (appointment is null) return Error.NotFound("Appointment.NotFound", "The appointment was not found.");
            appointment.UpdateNotes(notes); 

            await _context.SaveChangesAsync(ct);
            return Result.Success();
        }


        // 13. Submit Review
        public async Task<Result<Guid>> SubmitReviewAsync(Guid appointmentId, SubmitReviewRequest request, CancellationToken ct)
        {
            if (!_currentUser.IsAuthenticated || !_currentUser.Id.HasValue)
            {
                return Error.Unauthorized("Auth.Unauthorized", "User must be logged in to submit a review.");
            }

            var appointment = await _context.Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == appointmentId, ct);

            if (appointment is null)
            {
                return Error.NotFound("Appointment.NotFound", "The appointment was not found.");
            }


            if (appointment.UserId != _currentUser.Id.Value)
            {
                return Error.Forbidden("Review.Forbidden", "You can only review appointments that belong to you.");
            }


            bool alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.AppointmentId == appointmentId, ct);

            if (alreadyReviewed)
            {
                return Error.Conflict("Review.AlreadyExists", "You have already submitted a review for this appointment.");
            }

            var review = new Review
            {
                
                AppointmentId = appointmentId,
                ReviewerId = _currentUser.Id.Value,
                SpecialistId = appointment.SpecialistId,
                Rating = (byte)request.Rating,
                Comment = request.Comment
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync(ct);

            return review.Id;
        }

        // 14. Get Reminders for a Specific Appointment
        public async Task<Result<List<ReminderDto>>> GetRemindersAsync(Guid appointmentId, CancellationToken ct)
        {
            if (!_currentUser.Id.HasValue)
            {
                return Error.Unauthorized("Auth.Unauthorized", "User identification missing.");
            }

            var reminders = await _context.Reminders
                .AsNoTracking()
                .Where(r => r.AppointmentId == appointmentId && r.UserId == _currentUser.Id.Value)
                .Select(r => new ReminderDto
                {
                    Id = r.Id,
                    AppointmentId = r.AppointmentId,
                    ReminderTime = r.ReminderTime, 
                    Message = r.Message,
                    IsSent = r.IsSent
                })
                .ToListAsync(ct);

            return reminders;
        }

        // 15. Add New Reminder
        public async Task<Result<Guid>> AddReminderAsync(Guid appointmentId, AddReminderRequest request, CancellationToken ct)
        {
            if (!_currentUser.IsAuthenticated || !_currentUser.Id.HasValue)
            {
                return Error.Unauthorized("Auth.Unauthorized", "User must be logged in to create reminders.");
            }

            var appointment = await _context.Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == appointmentId, ct);

            if (appointment is null)
            {
                return Error.NotFound("Appointment.NotFound", "The appointment was not found.");
            }

            var reminder = new Reminder
            {
                AppointmentId = appointmentId,
                UserId = _currentUser.Id.Value,
                ReminderTime = request.ReminderTime.UtcDateTime,
                Message = request.Message,
                IsSent = false
            };

            _context.Reminders.Add(reminder);
            await _context.SaveChangesAsync(ct);

            return reminder.Id;
        }

        // 16. Delete Specific Reminder
        public async Task<Result> DeleteReminderAsync(Guid appointmentId, Guid reminderId, CancellationToken ct)
        {
            if (!_currentUser.Id.HasValue)
            {
                return Error.Unauthorized("Auth.Unauthorized", "User identification missing.");
            }

            var reminder = await _context.Reminders
                .FirstOrDefaultAsync(r => r.Id == reminderId && r.AppointmentId == appointmentId, ct);

            if (reminder is null)
            {
                return Error.NotFound("Reminder.NotFound", "The requested reminder was not found.");
            }

            if (reminder.UserId != _currentUser.Id.Value)
            {
                return Error.Forbidden("Reminder.Forbidden", "You do not have permission to delete this reminder.");
            }

            _context.Reminders.Remove(reminder);
            await _context.SaveChangesAsync(ct);

            return Result.Success();
        }

        public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
        {
            if (!_currentUser.Id.HasValue)
            {
                return Error.Unauthorized("Auth.Unauthorized", "User identification missing.");
            }

            var specialistId = await _context.Specialists
                .Where(s => s.UserId == _currentUser.Id.Value)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);

            if (!specialistId.HasValue)
            {
                return Error.Forbidden("Auth.NotSpecialist", "Only specialists can delete appointments.");
            }

            var appointment = await _context.Appointments
                .Include(a => a.Payment)
                .FirstOrDefaultAsync(a => a.Id == id && a.SpecialistId == specialistId.Value, ct);

            if (appointment is null)
            {
                return Error.NotFound("Appointment.NotFound", "The requested appointment was not found.");
            }

            if (appointment.Status != AppointmentStatus.Cancelled)
            {
                return Error.Validation("Appointment.InvalidStatus",
                    "Only cancelled or rejected appointments can be deleted.");
            }

            // Delete related Payment manually (Restrict FK)
            if (appointment.Payment is not null)
            {
                _context.Payments.Remove(appointment.Payment);
            }

            // Delete related Notifications
            var notifications = await _context.Notifications
                .Where(n => n.AppointmentId == id)
                .ToListAsync(ct);
            if (notifications.Count > 0)
            {
                _context.Notifications.RemoveRange(notifications);
            }

            // Delete the appointment (cascade removes StatusHistory and Reminders)
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync(ct);

            return Result.Success();
        }
    }
}