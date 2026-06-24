using Asp.Versioning;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.Application.Features.Admin.DTOs;
using BoslaPlatform.Application.Features.Admin.Requests;
using BoslaPlatform.Application.Features.Admin.Services;
using BoslaPlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoslaPlatform.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
//[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController(
    IAdminService adminService,
    BoslaPlatform.Application.Interfaces.AI.IEmbeddingAdminService embeddingAdmin) : ControllerBase
{
    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResponse<List<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IResult> ListUsers(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await adminService.ListUsersAsync(page, pageSize, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<List<UserDto>>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDetailsDto>), StatusCodes.Status200OK)]
    public async Task<IResult> GetUser(Guid id, CancellationToken ct = default)
    {
        var result = await adminService.GetUserByIdAsync(id, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<UserDetailsDto>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpPut("users/{id:guid}/roles")]
    public async Task<IResult> UpdateRoles(Guid id, [FromBody] UpdateUserRolesRequest request, CancellationToken ct = default)
    {
        var result = await adminService.UpdateUserRolesAsync(id, request.Roles, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Roles updated successfully."));
        return result.Errors.ToProblem();
    }

    [HttpPost("users/{id:guid}/deactivate")]
    public async Task<IResult> DeactivateUser(Guid id, CancellationToken ct = default)
    {
        var result = await adminService.DeactivateUserAsync(id, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("User deactivated."));
        return result.Errors.ToProblem();
    }

    [HttpPost("users/{id:guid}/reactivate")]
    public async Task<IResult> ReactivateUser(Guid id, CancellationToken ct = default)
    {
        var result = await adminService.ReactivateUserAsync(id, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("User reactivated."));
        return result.Errors.ToProblem();
    }

    [HttpPost("specialists/{id:guid}/verify")]
    public async Task<IResult> VerifySpecialist(Guid id, [FromBody] VerifySpecialistRequest request, CancellationToken ct = default)
    {
        var result = await adminService.VerifySpecialistAsync(id, request.IsVerified, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Specialist verification updated."));
        return result.Errors.ToProblem();
    }

    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(ApiResponse<List<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<IResult> GetAuditLogs(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await adminService.GetAuditLogsAsync(page, pageSize, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<List<AuditLogDto>>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpGet("audit-logs/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IResult> GetAuditLog(Guid id, CancellationToken ct = default)
    {
        var result = await adminService.GetAuditLogByIdAsync(id, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<AuditLogDto>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpGet("specialists/pending")]
    [ProducesResponseType(typeof(ApiResponse<List<SpecialistDto>>), StatusCodes.Status200OK)]
    public async Task<IResult> GetPendingSpecialists(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await adminService.GetPendingSpecialistsAsync(page, pageSize, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<List<SpecialistDto>>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpGet("specialists/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SpecialistDetailsDto>), StatusCodes.Status200OK)]
    public async Task<IResult> GetSpecialist(Guid id, CancellationToken ct = default)
    {
        var result = await adminService.GetSpecialistDetailAsync(id, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<SpecialistDetailsDto>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpGet("appointments")]
    [ProducesResponseType(typeof(ApiResponse<List<AppointmentDto>>), StatusCodes.Status200OK)]
    public async Task<IResult> GetAppointments(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await adminService.GetAllAppointmentsAsync(page, pageSize, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<List<AppointmentDto>>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpGet("ai/embeddings")]
    public async Task<IResult> GetEmbeddingStatus(CancellationToken ct = default)
    {
        var result = await embeddingAdmin.GetStatusAsync(ct);
        return result.Match(
            value => Results.Ok(ApiResponse<object>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpPost("ai/embeddings/rebuild")]
    public async Task<IResult> RebuildEmbeddings(CancellationToken ct = default)
    {
        var result = await embeddingAdmin.RebuildAllAsync(ct);
        if (result.IsSuccess) return Results.Ok(ApiResponse.SuccessResponse("Rebuild started."));
        return result.Errors.ToProblem();
    }

    [HttpPost("appointments/{id:guid}/cancel")]
    public async Task<IResult> CancelAppointment(Guid id, [FromBody] CancelAppointmentRequest request, CancellationToken ct = default)
    {
        var result = await adminService.CancelAppointmentAsync(id, request.Reason, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Appointment cancelled."));
        return result.Errors.ToProblem();
    }

    [HttpPost("appointments/{id:guid}/reschedule")]
    public async Task<IResult> RescheduleAppointment(Guid id, [FromBody] RescheduleAppointmentRequest request, CancellationToken ct = default)
    {
        var result = await adminService.RescheduleAppointmentAsync(id, request.NewStart, request.NewEnd, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Appointment rescheduled."));
        return result.Errors.ToProblem();
    }

    [HttpGet("payments")]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentDto>>), StatusCodes.Status200OK)]
    public async Task<IResult> GetPayments(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await adminService.GetAllPaymentsAsync(page, pageSize, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<List<PaymentDto>>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpPost("payments/{id:guid}/refund")]
    public async Task<IResult> RefundPayment(Guid id, CancellationToken ct = default)
    {
        var result = await adminService.RefundPaymentAsync(id, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Refund processed."));
        return result.Errors.ToProblem();
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<DashboardDto>), StatusCodes.Status200OK)]
    public async Task<IResult> GetDashboard(CancellationToken ct = default)
    {
        var result = await adminService.GetDashboardAsync(ct);
        return result.Match(
            value => Results.Ok(ApiResponse<DashboardDto>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }
}
