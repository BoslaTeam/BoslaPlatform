using Asp.Versioning;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.Application.Features.Admin.DTOs;
using BoslaPlatform.Application.Features.Admin.Requests;
using BoslaPlatform.Application.Features.Admin.Services;
using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Application.Features.Lookup.Response;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace BoslaPlatform.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController(
    IAdminService adminService, IEmbeddingAdminService embeddingAdmin, IUser currentUser) : ControllerBase
{
    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResponse<BoslaPlatform.Shared.PaginatedList<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IResult> ListUsers(int page = 1, int pageSize = 20, string? search = null, int? role = null, bool? isActive = null, CancellationToken ct = default)
    {
        var result = await adminService.ListUsersAsync(page, pageSize, search, role, isActive, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<BoslaPlatform.Shared.PaginatedList<UserDto>>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpPost("users")]
    public async Task<IResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct = default)
    {
        var result = await adminService.CreateUserAsync(request, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("User created successfully."));
        return result.Errors.ToProblem();
    }

    [HttpPut("users/{id:guid}")]
    public async Task<IResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct = default)
    {
        var result = await adminService.UpdateUserAsync(id, request, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("User updated successfully."));
        return result.Errors.ToProblem();
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

    [HttpGet("specialists")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<AdminSpecialistListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IResult> ListSpecialists(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? verificationStatus = null,
        CancellationToken ct = default)
    {
        var result = await adminService.ListSpecialistsAsync(page, pageSize, search, verificationStatus, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<PaginatedList<AdminSpecialistListItemDto>>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpGet("specialists/pending")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<AdminSpecialistListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IResult> ListPendingSpecialists(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var result = await adminService.ListPendingSpecialistsAsync(page, pageSize, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<PaginatedList<AdminSpecialistListItemDto>>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpGet("specialists/{id:guid}/detail")]
    [ProducesResponseType(typeof(ApiResponse<AdminSpecialistDetailDto>), StatusCodes.Status200OK)]
    public async Task<IResult> GetSpecialistDetail(Guid id, CancellationToken ct = default)
    {
        var result = await adminService.GetSpecialistDetailAsync(id, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<AdminSpecialistDetailDto>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpPost("specialists/{id:guid}/verify")]
    public async Task<IResult> VerifySpecialist(Guid id, [FromBody] VerifySpecialistRequest request, CancellationToken ct = default)
    {
        var currentUserId = currentUser.Id;
        if (currentUserId == null)
            return Results.Unauthorized();

        var result = await adminService.VerifySpecialistAsync(id, request.IsVerified, currentUserId.Value, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Specialist verification updated."));
        return result.Errors.ToProblem();
    }

    [HttpPut("specialists/{id:guid}/status")]
    public async Task<IResult> UpdateSpecialistStatus(Guid id, [FromBody] UpdateSpecialistStatusRequest request, CancellationToken ct = default)
    {
        var currentUserId = currentUser.Id;
        var result = await adminService.UpdateSpecialistStatusAsync(id, request.Status, currentUserId, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Specialist status updated."));
        return result.Errors.ToProblem();
    }

    [HttpPost("specialists")]
    public async Task<IResult> CreateSpecialist([FromBody] CreateSpecialistRequest request, CancellationToken ct = default)
    {
        var result = await adminService.CreateSpecialistAsync(request, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse<Guid>.SuccessResponse(result.Value));
        return result.Errors.ToProblem();
    }

    [HttpPut("specialists/{id:guid}")]
    public async Task<IResult> UpdateSpecialist(Guid id, [FromBody] AdminUpdateSpecialistRequest request, CancellationToken ct = default)
    {
        var result = await adminService.UpdateSpecialistAsync(id, request, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Specialist updated successfully."));
        return result.Errors.ToProblem();
    }

    // ── Appointments ──

    [HttpGet("appointments")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<AdminAppointmentDto>>), StatusCodes.Status200OK)]
    public async Task<IResult> ListAppointments(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        int? status = null,
        CancellationToken ct = default)
    {
        var result = await adminService.ListAppointmentsAsync(page, pageSize, search, status, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<PaginatedList<AdminAppointmentDto>>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpGet("appointments/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AdminAppointmentDetailDto>), StatusCodes.Status200OK)]
    public async Task<IResult> GetAppointmentDetail(Guid id, CancellationToken ct = default)
    {
        var result = await adminService.GetAppointmentDetailAsync(id, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<AdminAppointmentDetailDto>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpPost("appointments/{id:guid}/cancel")]
    public async Task<IResult> CancelAppointment(Guid id, [FromBody] CancelAppointmentRequest request, CancellationToken ct = default)
    {
        var result = await adminService.CancelAppointmentAsync(id, request.Reason, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Appointment cancelled."));
        return result.Errors.ToProblem();
    }

    [HttpPost("appointments/{id:guid}/confirm")]
    public async Task<IResult> ConfirmAppointment(Guid id, CancellationToken ct = default)
    {
        var result = await adminService.ConfirmAppointmentAsync(id, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Appointment confirmed."));
        return result.Errors.ToProblem();
    }

    [HttpPost("appointments/{id:guid}/complete")]
    public async Task<IResult> CompleteAppointment(Guid id, CancellationToken ct = default)
    {
        var result = await adminService.CompleteAppointmentAsync(id, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Appointment completed."));
        return result.Errors.ToProblem();
    }

    // ── Expertise ──

    [HttpGet("expertise")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IResult> ListExpertise(CancellationToken ct = default)
    {
        var result = await adminService.GetExpertiseListAsync(ct);
        return result.Match(
            value => Results.Ok(ApiResponse<List<LookupItemResponse>>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpPost("expertise")]
    public async Task<IResult> CreateExpertise([FromBody] CreateLookupItemRequest request, CancellationToken ct = default)
    {
        var result = await adminService.CreateExpertiseAsync(request.Name, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse<Guid>.SuccessResponse(result.Value));
        return result.Errors.ToProblem();
    }

    [HttpPut("expertise/{id:guid}")]
    public async Task<IResult> UpdateExpertise(Guid id, [FromBody] UpdateLookupItemRequest request, CancellationToken ct = default)
    {
        var result = await adminService.UpdateExpertiseAsync(id, request.Name, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Expertise updated."));
        return result.Errors.ToProblem();
    }

    [HttpDelete("expertise/{id:guid}")]
    public async Task<IResult> DeleteExpertise(Guid id, CancellationToken ct = default)
    {
        var result = await adminService.DeleteExpertiseAsync(id, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Expertise deleted."));
        return result.Errors.ToProblem();
    }

    // ── Skills ──

    [HttpGet("skills")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IResult> ListSkills(CancellationToken ct = default)
    {
        var result = await adminService.GetSkillListAsync(ct);
        return result.Match(
            value => Results.Ok(ApiResponse<List<LookupItemResponse>>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpPost("skills")]
    public async Task<IResult> CreateSkill([FromBody] CreateLookupItemRequest request, CancellationToken ct = default)
    {
        var result = await adminService.CreateSkillAsync(request.Name, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse<Guid>.SuccessResponse(result.Value));
        return result.Errors.ToProblem();
    }

    [HttpPut("skills/{id:guid}")]
    public async Task<IResult> UpdateSkill(Guid id, [FromBody] UpdateLookupItemRequest request, CancellationToken ct = default)
    {
        var result = await adminService.UpdateSkillAsync(id, request.Name, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Skill updated."));
        return result.Errors.ToProblem();
    }

    [HttpDelete("skills/{id:guid}")]
    public async Task<IResult> DeleteSkill(Guid id, CancellationToken ct = default)
    {
        var result = await adminService.DeleteSkillAsync(id, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Skill deleted."));
        return result.Errors.ToProblem();
    }

    // ── Tools ──

    [HttpGet("tools")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IResult> ListTools(CancellationToken ct = default)
    {
        var result = await adminService.GetToolListAsync(ct);
        return result.Match(
            value => Results.Ok(ApiResponse<List<LookupItemResponse>>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpPost("tools")]
    public async Task<IResult> CreateTool([FromBody] CreateLookupItemRequest request, CancellationToken ct = default)
    {
        var result = await adminService.CreateToolAsync(request.Name, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse<Guid>.SuccessResponse(result.Value));
        return result.Errors.ToProblem();
    }

    [HttpPut("tools/{id:guid}")]
    public async Task<IResult> UpdateTool(Guid id, [FromBody] UpdateLookupItemRequest request, CancellationToken ct = default)
    {
        var result = await adminService.UpdateToolAsync(id, request.Name, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Tool updated."));
        return result.Errors.ToProblem();
    }

    [HttpDelete("tools/{id:guid}")]
    public async Task<IResult> DeleteTool(Guid id, CancellationToken ct = default)
    {
        var result = await adminService.DeleteToolAsync(id, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Tool deleted."));
        return result.Errors.ToProblem();
    }

    // ── Industries ──

    [HttpGet("industries")]
    [ProducesResponseType(typeof(ApiResponse<List<LookupItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IResult> ListIndustries(CancellationToken ct = default)
    {
        var result = await adminService.GetIndustryListAsync(ct);
        return result.Match(
            value => Results.Ok(ApiResponse<List<LookupItemResponse>>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpPost("industries")]
    public async Task<IResult> CreateIndustry([FromBody] CreateLookupItemRequest request, CancellationToken ct = default)
    {
        var result = await adminService.CreateIndustryAsync(request.Name, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse<Guid>.SuccessResponse(result.Value));
        return result.Errors.ToProblem();
    }

    [HttpPut("industries/{id:guid}")]
    public async Task<IResult> UpdateIndustry(Guid id, [FromBody] UpdateLookupItemRequest request, CancellationToken ct = default)
    {
        var result = await adminService.UpdateIndustryAsync(id, request.Name, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Industry updated."));
        return result.Errors.ToProblem();
    }

    [HttpDelete("industries/{id:guid}")]
    public async Task<IResult> DeleteIndustry(Guid id, CancellationToken ct = default)
    {
        var result = await adminService.DeleteIndustryAsync(id, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Industry deleted."));
        return result.Errors.ToProblem();
    }

    // ── Payments ──

    [HttpGet("payments")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<AdminPaymentDto>>), StatusCodes.Status200OK)]
    public async Task<IResult> ListPayments(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? status = null,
        CancellationToken ct = default)
    {
        var result = await adminService.ListPaymentsAsync(page, pageSize, search, status, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<PaginatedList<AdminPaymentDto>>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpGet("payments/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AdminPaymentDetailDto>), StatusCodes.Status200OK)]
    public async Task<IResult> GetPaymentDetail(Guid id, CancellationToken ct = default)
    {
        var result = await adminService.GetPaymentDetailAsync(id, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<AdminPaymentDetailDto>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpPost("payments/{id:guid}/refund")]
    public async Task<IResult> RefundPayment(Guid id, [FromBody] RefundPaymentRequest request, CancellationToken ct = default)
    {
        var result = await adminService.RefundPaymentAsync(id, request.Reason, ct);
        if (result.IsSuccess)
            return Results.Ok(ApiResponse.SuccessResponse("Payment refunded."));
        return result.Errors.ToProblem();
    }

    [HttpGet("audit-logs/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IResult> GetAuditLogById(Guid id, CancellationToken ct = default)
    {
        var result = await adminService.GetAuditLogByIdAsync(id, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<AuditLogDto>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<IResult> GetAuditLogs(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        string? action = null,
        string? entityType = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var result = await adminService.GetAuditLogsAsync(page, pageSize, search, action, entityType, from, to, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<PaginatedList<AuditLogDto>>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpGet("dashboardstats")]
    [ProducesResponseType(typeof(ApiResponse<AdminDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IResult> GetDashboardStats(CancellationToken ct = default)
    {
        var result = await adminService.GetDashboardStatsAsync(ct);
        return result.Match(
            value => Results.Ok(ApiResponse<AdminDashboardDto>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    // ── AI Embeddings ──

    [HttpGet("ai/embeddings")]
    [ProducesResponseType(typeof(ApiResponse<EmbeddingsStatusDto>), StatusCodes.Status200OK)]
    public async Task<IResult> GetEmbeddingsStatus(CancellationToken ct = default)
    {
        var result = await adminService.GetEmbeddingsStatusAsync(ct);
        return result.Match(
            value => Results.Ok(ApiResponse<EmbeddingsStatusDto>.SuccessResponse(value)),
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

    [HttpGet("specialists/thepending")]
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
        var result = await adminService.GetTheSpecialistDetailAsync(id, ct);
        return result.Match(
            value => Results.Ok(ApiResponse<SpecialistDetailsDto>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }

    [HttpGet("appointments/list")]
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

    [HttpPost("appointments/{id:guid}/delete")]
    public async Task<IResult> CancelTheAppointment(Guid id, [FromBody] CancelAppointmentRequest request, CancellationToken ct = default)
    {
        var result = await adminService.CancelAppointmentAsync(id, request.Reason , ct);
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
