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

        Task<Result<Guid>> CreateAsync(CreateAppointmentRequest request, CancellationToken ct);

        Task<Result<AppointmentDto>> GetByIdAsync(Guid id, CancellationToken ct);


        Task<Result<PaginatedList<AppointmentDto>>> GetMyAppointmentsAsync(int pageNumber, int pageSize, CancellationToken ct);


        Task<Result<PaginatedList<AppointmentDto>>> GetSpecialistAppointmentsAsync(Guid specialistId, int pageNumber, int pageSize, CancellationToken ct);


        Task<Result<List<AppointmentDto>>> GetUpcomingAppointmentsAsync(CancellationToken ct);


        Task<Result<List<AppointmentStatusHistoryDto>>> GetStatusHistoryAsync(Guid id, CancellationToken ct);

 
        Task<Result> ConfirmAsync(Guid id, CancellationToken ct);


        Task<Result> CancelAsync(Guid id, string reason, CancellationToken ct);


        Task<Result> RescheduleAsync(Guid id, DateTimeOffset newStart, DateTimeOffset newEnd, string reason, CancellationToken ct);


        Task<Result> CompleteAsync(Guid id, CancellationToken ct);


        Task<Result> RejectAsync(Guid id, string reason, CancellationToken ct);

        Task<Result> UpdateNotesAsync(Guid id, string notes, CancellationToken ct);

        Task<Result<Guid>> SubmitReviewAsync(Guid appointmentId, SubmitReviewRequest request, CancellationToken ct);

        Task<Result<List<ReminderDto>>> GetRemindersAsync(Guid appointmentId, CancellationToken ct);

        Task<Result<Guid>> AddReminderAsync(Guid appointmentId, AddReminderRequest request, CancellationToken ct);

        Task<Result> DeleteReminderAsync(Guid appointmentId, Guid reminderId, CancellationToken ct);

    }
}