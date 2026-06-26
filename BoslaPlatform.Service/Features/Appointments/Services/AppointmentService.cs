using BoslaPlatform.Application.Features.Appointments.DTOs;
using BoslaPlatform.Application.Features.Appointments.Requests;
using BoslaPlatform.Application.Features.Appointments.Services;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Enums;
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

            var appointment = Appointment.Schedule(
                request.SpecialistId,
                currentUserId,
                request.Start,
                request.End,
                request.SessionTopic,
                request.Notes
            );

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync(ct);

            return appointment.Id;
        }

        // 2. Read Single Appointment
        public async Task<Result<AppointmentDto>> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var appointment = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    SpecialistId = a.SpecialistId,
                    UserId = a.UserId,
                    Start = a.Start,
                    End = a.End,
                    Status = a.Status,
                    SessionTopic = a.SessionTopic,
                    Notes = a.Notes
                })
                .FirstOrDefaultAsync(ct);

            if (appointment is null)
            {
                return Error.NotFound("Appointment.NotFound", "The requested appointment was not found.");
            }

            return appointment;
        }

        // 3. Read Paginated List for Logged-In Patient
        public async Task<Result<PaginatedList<AppointmentDto>>> GetMyAppointmentsAsync(int pageNumber, int pageSize, CancellationToken ct)
        {
            if (!_currentUser.Id.HasValue)
            {
                return Error.Unauthorized("Auth.Unauthorized", "User identification missing.");
            }

            Guid currentUserId = _currentUser.Id.Value;

            var query = _context.Appointments
                .AsNoTracking()
                .Where(a => a.UserId == currentUserId);

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
                    Notes = a.Notes
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
                    Notes = a.Notes
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

            // Fetch active upcoming appointments for either the patient or specialist
            var upcomingAppointments = await _context.Appointments
                .AsNoTracking()
                .Where(a => (a.UserId == currentUserId || a.SpecialistId == currentUserId) &&
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
                    Notes = a.Notes
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
        public async Task<Result> CancelAsync(Guid id, string reason, CancellationToken ct)
        {
            var appointment = await _context.Appointments
                .Include(a => a.StatusHistory)
                .FirstOrDefaultAsync(a => a.Id == id, ct);

            if (appointment is null) return Error.NotFound("Appointment.NotFound", "The appointment was not found.");

            if (!_currentUser.Id.HasValue) return Error.Unauthorized("Auth.Unauthorized", "User identification missing.");

            Guid executingUserId = _currentUser.Id.Value;

            var result = appointment.Cancel(executingUserId, reason);
            if (result.IsError) return result.Errors;

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




        public async Task<Result<Guid>> AddReviewAsync(Guid appointmentId,AddReviewRequest request,CancellationToken ct)
        {
            if (!_currentUser.IsAuthenticated || !_currentUser.Id.HasValue)
                return Error.Unauthorized(description: "User is not authenticated.");

            if (request.Rating < 1 || request.Rating > 5)
            {
                return Error.Validation(description: "Rating must be between 1 and 5.");
            }

            var appointment = await _context.Appointments
                .Include(x => x.Review)
                .FirstOrDefaultAsync(
                    x => x.Id == appointmentId,
                    ct);

            if (appointment is null)
                return Error.NotFound(description: "Appointment not found.");

            if (appointment.UserId != _currentUser.Id.Value)
                return Error.Forbidden(description: "You can only review your own appointments.");

            if (appointment.Status != AppointmentStatus.Completed)
                return Error.Validation(
                    description: "Only completed appointments can be reviewed.");

            if (appointment.Review is not null)
                return Error.Conflict(
                    description: "A review already exists for this appointment.");

            var review = new Review
            {
                AppointmentId = appointment.Id,
                ReviewerId = _currentUser.Id.Value,
                SpecialistId = appointment.SpecialistId,
                Rating = request.Rating,
                Comment = request.Comment
            };

            await _context.Reviews.AddAsync(review, ct);

            await _context.SaveChangesAsync(ct);

            return review.Id;
        }
    }
}