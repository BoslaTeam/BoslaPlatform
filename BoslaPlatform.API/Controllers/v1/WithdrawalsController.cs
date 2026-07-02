using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Withdrawals.DTOs;
using BoslaPlatform.Application.Interfaces;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace BoslaPlatform.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/withdrawals")]
[Authorize(Roles = "Specialist")]
public class WithdrawalsController(
    IWithdrawalService withdrawalService,
    IUser currentUser,
    IAppDbContext context) : ControllerBase
{
    [HttpGet("wallet")]
    public async Task<IResult> GetWallet()
    {
        var specialistId = await GetSpecialistIdAsync();
        if (specialistId is null)
            return Results.NotFound();

        var result = await withdrawalService.GetWalletAsync(specialistId.Value);
        if (result.IsError)
            return result.Errors.ToProblem();

        return Results.Ok(ApiResponse<WalletDto>.SuccessResponse(result.Value));
    }

    [HttpPost("request")]
    public async Task<IResult> RequestWithdrawal([FromBody] WithdrawRequestDto request)
    {
        var specialistId = await GetSpecialistIdAsync();
        if (specialistId is null)
            return Results.NotFound();

        var result = await withdrawalService.RequestWithdrawalAsync(specialistId.Value, request);
        if (result.IsError)
            return result.Errors.ToProblem();

        return Results.Ok(ApiResponse<WithdrawalDto>.SuccessResponse(result.Value));
    }

    [HttpGet("history")]
    public async Task<IResult> GetHistory()
    {
        var specialistId = await GetSpecialistIdAsync();
        if (specialistId is null)
            return Results.NotFound();

        var result = await withdrawalService.GetHistoryAsync(specialistId.Value);
        if (result.IsError)
            return result.Errors.ToProblem();

        return Results.Ok(ApiResponse<List<WithdrawalDto>>.SuccessResponse(result.Value));
    }

    private async Task<Guid?> GetSpecialistIdAsync()
    {
        if (!currentUser.Id.HasValue) return null;

        return await context.Specialists
            .Where(s => s.UserId == currentUser.Id.Value)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();
    }
}
