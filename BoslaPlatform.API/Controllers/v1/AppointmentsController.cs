using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BoslaPlatform.Application.Features.Appointments.Services;
using BoslaPlatform.Application.Features.Appointments.Requests;
using BoslaPlatform.Application.Features.Appointments.DTOs;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Shared;
using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Domain.Models;

namespace BoslaPlatform.API.Controllers.V1
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ISummaryService _summaryService;

        public AppointmentsController(IAppointmentService appointmentService, ISummaryService summaryService)
        {
            _appointmentService = appointmentService;
            _summaryService = summaryService;
        }

        // 1. POST: api/v1/appointments
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken ct)
        {
            var result = await _appointmentService.CreateAsync(request, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment scheduled successfully."));
        }

        // 2. GET: api/v1/appointments/{id}
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await _appointmentService.GetByIdAsync(id, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse());
        }

        // 3. GET: api/v1/appointments/my-appointments
        [HttpGet("my-appointments")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<AppointmentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyAppointments([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var result = await _appointmentService.GetMyAppointmentsAsync(pageNumber, pageSize, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());

            var response = ApiResponse<IReadOnlyCollection<AppointmentDto>>.PaginatedResponse(
                result.Value.Items,
                result.Value.Metadata,
                "Appointments retrieved successfully."
            );
            return Ok(response);
        }

        // 4. GET: api/v1/appointments/specialist/{specialistId}
        [HttpGet("specialist/{specialistId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<AppointmentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetSpecialistAppointments([FromRoute] Guid specialistId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var result = await _appointmentService.GetSpecialistAppointmentsAsync(specialistId, pageNumber, pageSize, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());

            var response = ApiResponse<IReadOnlyCollection<AppointmentDto>>.PaginatedResponse(
                result.Value.Items,
                result.Value.Metadata,
                "Specialist appointments retrieved successfully."
            );
            return Ok(response);
        }

        // 5. GET: api/v1/appointments/my-specialist-appointments
        [HttpGet("my-specialist-appointments")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<AppointmentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMySpecialistAppointments([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var result = await _appointmentService.GetMySpecialistAppointmentsAsync(pageNumber, pageSize, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());

            var response = ApiResponse<IReadOnlyCollection<AppointmentDto>>.PaginatedResponse(
                result.Value.Items,
                result.Value.Metadata,
                "Specialist appointments retrieved successfully."
            );
            return Ok(response);
        }

        // 6. GET: api/v1/appointments/upcoming
        [HttpGet("upcoming")]
        [ProducesResponseType(typeof(ApiResponse<List<AppointmentDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUpcomingAppointments(CancellationToken ct)
        {
            var result = await _appointmentService.GetUpcomingAppointmentsAsync(ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Upcoming appointments retrieved successfully."));
        }

        // 6. GET: api/v1/appointments/{id}/history
        [HttpGet("{id:guid}/history")]
        [ProducesResponseType(typeof(ApiResponse<List<AppointmentStatusHistoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStatusHistory([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await _appointmentService.GetStatusHistoryAsync(id, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment history retrieved successfully."));
        }

        // 7. PUT: api/v1/appointments/{id}/confirm-payment
        [HttpPut("{id:guid}/confirm-payment")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ConfirmPayment([FromRoute] Guid id, [FromBody] ConfirmPaymentRequest request, CancellationToken ct)
        {
            var result = await _appointmentService.ConfirmPaymentAsync(id, request.PaymentIntentId, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Payment confirmed and appointment marked as paid successfully."));
        }

        // 8. PUT: api/v1/appointments/{id}/confirm
        [HttpPut("{id:guid}/confirm")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Confirm([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await _appointmentService.ConfirmAsync(id, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment confirmed successfully."));
        }

        // 9. PUT: api/v1/appointments/{id}/cancel
        [HttpPut("{id:guid}/cancel")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel([FromRoute] Guid id, [FromBody] CancelAppointmentRequest request, CancellationToken ct)
        {
            var result = await _appointmentService.CancelAsync(id, request.Reason, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment cancelled successfully."));
        }

        // 10. PUT: api/v1/appointments/{id}/reschedule
        [HttpPut("{id:guid}/reschedule")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Reschedule([FromRoute] Guid id, [FromBody] RescheduleAppointmentRequest request, CancellationToken ct)
        {
            var result = await _appointmentService.RescheduleAsync(id, request.NewStart, request.NewEnd, request.Reason, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment rescheduled successfully."));
        }

        // 11. PUT: api/v1/appointments/{id}/complete
        [HttpPut("{id:guid}/complete")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Complete([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await _appointmentService.CompleteAsync(id, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment marked as completed successfully."));
        }

        // 12. PUT: api/v1/appointments/{id}/reject
        [HttpPut("{id:guid}/reject")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Reject([FromRoute] Guid id, [FromBody] RejectAppointmentRequest request, CancellationToken ct)
        {
            var result = await _appointmentService.RejectAsync(id, request.Reason, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment request rejected successfully."));
        }

        // 13. PATCH: api/v1/appointments/{id}/notes
        [HttpPatch("{id:guid}/notes")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNotes([FromRoute] Guid id, [FromBody] UpdateAppointmentNotesRequest request, CancellationToken ct)
        {
            var result = await _appointmentService.UpdateNotesAsync(id, request.Notes, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment notes updated successfully."));
        }

        // 14. POST: api/v1/appointments/{id}/reviews

        [HttpPost("{id:guid}/reviews")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SubmitReview([FromRoute] Guid id, [FromBody] SubmitReviewRequest request, CancellationToken ct)
        {
            var result = await _appointmentService.SubmitReviewAsync(id, request, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Review submitted successfully."));
        }

        // 15. GET: api/v1/appointments/{id}/reminders

        [HttpGet("{id:guid}/reminders")]
        [ProducesResponseType(typeof(ApiResponse<List<ReminderDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetReminders([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await _appointmentService.GetRemindersAsync(id, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Reminders retrieved successfully."));
        }

        // 16. POST: api/v1/appointments/{id}/reminders

        [HttpPost("{id:guid}/reminders")]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status410Gone)]
        public async Task<IActionResult> AddReminder([FromRoute] Guid id, [FromBody] AddReminderRequest request, CancellationToken ct)
        {
            var result = await _appointmentService.AddReminderAsync(id, request, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Reminder added successfully."));
        }

        // 17. DELETE: api/v1/appointments/{id} (cancelled/rejected only)

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await _appointmentService.DeleteAsync(id, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment deleted successfully."));
        }

        // 18. DELETE: api/v1/appointments/{id}/reminders/{rid}

        [HttpDelete("{id:guid}/reminders/{rid:guid}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteReminder([FromRoute] Guid id, [FromRoute] Guid rid, CancellationToken ct)
        {
            var result = await _appointmentService.DeleteReminderAsync(id, rid, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Reminder deleted successfully."));
        }

        [HttpGet("{id:guid}/summary")]
        [ProducesResponseType(typeof(ApiResponse<BoslaPlatform.Application.Features.Appointments.DTOs.SessionSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetSummary([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await _summaryService.GetAsync(id, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());

            var summaryDto = new SessionSummaryDto
            {
                Id = result.Value.Id,
                AppointmentId = result.Value.AppointmentId,
                TranscriptId = result.Value.TranscriptId,
                KeyTakeaways = result.Value.KeyTakeaways,
                ActionItemsForUser = result.Value.ActionItemsForUser,
                ActionItemsForSpec = result.Value.ActionItemsForSpec,
                LlmProvider = result.Value.LlmProvider,
                Status = result.Value.Status,
                CreatedAtUtc = result.Value.CreatedAtUtc,
                CreatedBy = result.Value.CreatedBy,
                LastModifiedUtc = result.Value.LastModifiedUtc,
                LastModifiedBy = result.Value.LastModifiedBy
            };

            return Ok(ApiResponse<SessionSummaryDto>.SuccessResponse(summaryDto, "Summary retrieved successfully."));
        }

        private IActionResult DetermineStatusCode(ErrorKind type, object responseBody)
        {
            return type switch
            {
                ErrorKind.NotFound => NotFound(responseBody),
                ErrorKind.Validation => BadRequest(responseBody),
                ErrorKind.BadRequest => BadRequest(responseBody),
                ErrorKind.Conflict => Conflict(responseBody),
                ErrorKind.Unauthorized => Unauthorized(responseBody),
                ErrorKind.Forbidden => Forbid(),
                _ => StatusCode(500, responseBody)
            };
        }


    }
}