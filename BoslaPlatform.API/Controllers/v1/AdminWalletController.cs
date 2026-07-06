using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Wallets.DTOs;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Wallets;
using BoslaPlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace BoslaPlatform.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/wallet")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminWalletController(
    IWalletService walletService,
    IUser currentUser) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IResult> GetPlatformStats()
    {
        var result = await walletService.GetAdminWalletStatsAsync();
        if (result.IsError) return result.Errors.ToProblem();
        return Results.Ok(ApiResponse<AdminWalletStatsDto>.SuccessResponse(result.Value));
    }

    [HttpGet("transactions")]
    public async Task<IResult> GetAllTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await walletService.GetAllTransactionsAsync(page, pageSize);
        if (result.IsError) return result.Errors.ToProblem();
        return Results.Ok(ApiResponse<List<TransactionDto>>.SuccessResponse(result.Value));
    }

    [HttpGet("me")]
    public async Task<IResult> GetAdminWallet()
    {
        if (!currentUser.Id.HasValue) return Results.Unauthorized();
        var result = await walletService.GetAdminWalletAsync(currentUser.Id.Value);
        if (result.IsError) return result.Errors.ToProblem();
        return Results.Ok(ApiResponse<WalletResponseDto>.SuccessResponse(result.Value));
    }
}
