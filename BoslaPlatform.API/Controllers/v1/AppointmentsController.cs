using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BoslaPlatform.Application.Features.Appointments.Services;
using BoslaPlatform.Application.Features.Appointments.Requests;
using BoslaPlatform.Application.Features.Appointments.DTOs;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Shared;

namespace BoslaPlatform.API.Controllers.V1
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        // 1. POST: api/v1/appointments
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken ct)
        {
            var result = await _appointmentService.CreateAsync(request, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment scheduled successfully."));
        }

        // 2. GET: api/v1/appointments/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await _appointmentService.GetByIdAsync(id, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse());
        }

        // 3. GET: api/v1/appointments/my-appointments
        [HttpGet("my-appointments")]
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

        // 5. GET: api/v1/appointments/upcoming
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingAppointments(CancellationToken ct)
        {
            var result = await _appointmentService.GetUpcomingAppointmentsAsync(ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Upcoming appointments retrieved successfully."));
        }

        // 6. GET: api/v1/appointments/{id}/history
        [HttpGet("{id:guid}/history")]
        public async Task<IActionResult> GetStatusHistory([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await _appointmentService.GetStatusHistoryAsync(id, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment history retrieved successfully."));
        }

        // 7. PUT: api/v1/appointments/{id}/confirm
        [HttpPut("{id:guid}/confirm")]
        public async Task<IActionResult> Confirm([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await _appointmentService.ConfirmAsync(id, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment confirmed successfully."));
        }

        // 8. PUT: api/v1/appointments/{id}/cancel
        [HttpPut("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel([FromRoute] Guid id, [FromBody] CancelAppointmentRequest request, CancellationToken ct)
        {
            var result = await _appointmentService.CancelAsync(id, request.Reason, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment cancelled successfully."));
        }

        // 9. PUT: api/v1/appointments/{id}/reschedule
        [HttpPut("{id:guid}/reschedule")]
        public async Task<IActionResult> Reschedule([FromRoute] Guid id, [FromBody] RescheduleAppointmentRequest request, CancellationToken ct)
        {
            var result = await _appointmentService.RescheduleAsync(id, request.NewStart, request.NewEnd, request.Reason, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment rescheduled successfully."));
        }

        // 10. PUT: api/v1/appointments/{id}/complete
        [HttpPut("{id:guid}/complete")]
        public async Task<IActionResult> Complete([FromRoute] Guid id, CancellationToken ct)
        {
            var result = await _appointmentService.CompleteAsync(id, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment marked as completed successfully."));
        }

        // 11. PUT: api/v1/appointments/{id}/reject
        [HttpPut("{id:guid}/reject")]
        public async Task<IActionResult> Reject([FromRoute] Guid id, [FromBody] RejectAppointmentRequest request, CancellationToken ct)
        {
            var result = await _appointmentService.RejectAsync(id, request.Reason, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment request rejected successfully."));
        }

        // 12. PATCH: api/v1/appointments/{id}/notes
        [HttpPatch("{id:guid}/notes")]
        public async Task<IActionResult> UpdateNotes([FromRoute] Guid id, [FromBody] UpdateAppointmentNotesRequest request, CancellationToken ct)
        {
            var result = await _appointmentService.UpdateNotesAsync(id, request.Notes, ct);
            if (result.IsError) return DetermineStatusCode(result.Errors[0].Type, result.ToApiResponse());
            return Ok(result.ToApiResponse("Appointment notes updated successfully."));
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