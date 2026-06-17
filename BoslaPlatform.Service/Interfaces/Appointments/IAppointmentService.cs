using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BoslaPlatform.Application.Features.Appointments.DTOs;
using BoslaPlatform.Application.Features.Appointments.Requests;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Features.Appointments.Services
{
    public interface IAppointmentService
    {
        // 1. Create
        Task<Result<Guid>> CreateAsync(CreateAppointmentRequest request, CancellationToken ct);

        // 2. Read Single
        Task<Result<AppointmentDto>> GetByIdAsync(Guid id, CancellationToken ct);

        // 3. Read Paginated for Patient
        Task<Result<PaginatedList<AppointmentDto>>> GetMyAppointmentsAsync(int pageNumber, int pageSize, CancellationToken ct);

        // 4. Read Paginated for Specialist
        Task<Result<PaginatedList<AppointmentDto>>> GetSpecialistAppointmentsAsync(Guid specialistId, int pageNumber, int pageSize, CancellationToken ct);

        // 5. Get Upcoming Appointments
        Task<Result<List<AppointmentDto>>> GetUpcomingAppointmentsAsync(CancellationToken ct);

        // 6. Get Appointment Status History
        Task<Result<List<AppointmentStatusHistoryDto>>> GetStatusHistoryAsync(Guid id, CancellationToken ct);

        // 7. Confirm
        Task<Result> ConfirmAsync(Guid id, CancellationToken ct);

        // 8. Cancel
        Task<Result> CancelAsync(Guid id, string reason, CancellationToken ct);

        // 9. Reschedule
        Task<Result> RescheduleAsync(Guid id, DateTimeOffset newStart, DateTimeOffset newEnd, string reason, CancellationToken ct);

        // 10. Complete
        Task<Result> CompleteAsync(Guid id, CancellationToken ct);

        // 11. Reject/Decline Appointment Request
        Task<Result> RejectAsync(Guid id, string reason, CancellationToken ct);

        // 12. Update Notes
        Task<Result> UpdateNotesAsync(Guid id, string notes, CancellationToken ct);
    }
}