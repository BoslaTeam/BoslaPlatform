using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Withdrawals.DTOs;
using BoslaPlatform.Application.Interfaces;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace BoslaPlatform.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/withdrawals")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminWithdrawalsController(
    IWithdrawalService withdrawalService,
    IUser currentUser) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<IResult> GetPending()
    {
        var result = await withdrawalService.GetPendingWithdrawalsAsync();
        if (result.IsError)
            return result.Errors.ToProblem();

        return Results.Ok(ApiResponse<List<WithdrawalListDto>>.SuccessResponse(result.Value));
    }

    [HttpGet]
    public async Task<IResult> GetAll([FromQuery] string? status = null)
    {
        var result = await withdrawalService.GetAllWithdrawalsAsync(status);
        if (result.IsError)
            return result.Errors.ToProblem();

        return Results.Ok(ApiResponse<List<WithdrawalListDto>>.SuccessResponse(result.Value));
    }

    [HttpGet("{id:guid}")]
    public async Task<IResult> GetDetail(Guid id)
    {
        var result = await withdrawalService.GetWithdrawalDetailAsync(id);
        if (result.IsError)
            return result.Errors.ToProblem();

        return Results.Ok(ApiResponse<WithdrawalDetailDto>.SuccessResponse(result.Value));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IResult> Approve(Guid id, [FromBody] AdminWithdrawActionDto? request)
    {
        if (!currentUser.Id.HasValue)
            return Results.Unauthorized();

        var result = await withdrawalService.ApproveWithdrawalAsync(id, currentUser.Id.Value, request?.AdminNotes);
        if (result.IsError)
            return result.Errors.ToProblem();

        return Results.Ok(ApiResponse.SuccessResponse("Withdrawal approved and marked as processing."));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IResult> Reject(Guid id, [FromBody] AdminWithdrawActionDto? request)
    {
        if (!currentUser.Id.HasValue)
            return Results.Unauthorized();

        var result = await withdrawalService.RejectWithdrawalAsync(id, currentUser.Id.Value, request?.AdminNotes);
        if (result.IsError)
            return result.Errors.ToProblem();

        return Results.Ok(ApiResponse.SuccessResponse("Withdrawal rejected."));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IResult> MarkCompleted(Guid id)
    {
        var result = await withdrawalService.MarkCompletedAsync(id);
        if (result.IsError)
            return result.Errors.ToProblem();

        return Results.Ok(ApiResponse.SuccessResponse("Withdrawal completed."));
    }
}
