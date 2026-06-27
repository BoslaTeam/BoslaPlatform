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
    IAdminService adminService) : ControllerBase
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

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<AdminDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IResult> GetDashboardStats(CancellationToken ct = default)
    {
        var result = await adminService.GetDashboardStatsAsync(ct);
        return result.Match(
            value => Results.Ok(ApiResponse<AdminDashboardDto>.SuccessResponse(value)),
            errors => errors.ToProblem());
    }
}
