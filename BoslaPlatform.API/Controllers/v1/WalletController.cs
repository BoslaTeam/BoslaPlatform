using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Wallets.DTOs;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Wallets;
using BoslaPlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace BoslaPlatform.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/wallet")]
[Authorize]
public class WalletController(
    IWalletService walletService,
    IUser currentUser,
    IAppDbContext context) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IResult> GetMyWallet()
    {
        if (!currentUser.Id.HasValue)
            return Results.Unauthorized();

        var role = currentUser.Role;

        if (role == nameof(UserRole.Specialist))
        {
            var specialistId = await context.Specialists
                .Where(s => s.UserId == currentUser.Id.Value)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync();

            if (specialistId is null)
                return Results.NotFound();

            var result = await walletService.GetSpecialistWalletAsync(specialistId.Value);
            if (result.IsError) return result.Errors.ToProblem();
            return Results.Ok(ApiResponse<WalletResponseDto>.SuccessResponse(result.Value));
        }

        if (role == nameof(UserRole.Admin))
        {
            var result = await walletService.GetAdminWalletAsync(currentUser.Id.Value);
            if (result.IsError) return result.Errors.ToProblem();
            return Results.Ok(ApiResponse<WalletResponseDto>.SuccessResponse(result.Value));
        }

        {
            var result = await walletService.GetUserWalletAsync(currentUser.Id.Value);
            if (result.IsError) return result.Errors.ToProblem();
            return Results.Ok(ApiResponse<WalletResponseDto>.SuccessResponse(result.Value));
        }
    }

    [HttpGet("me/transactions")]
    public async Task<IResult> GetMyTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!currentUser.Id.HasValue)
            return Results.Unauthorized();

        var role = currentUser.Role;

        if (role == nameof(UserRole.Specialist))
        {
            var specialistId = await context.Specialists
                .Where(s => s.UserId == currentUser.Id.Value)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync();

            if (specialistId is null) return Results.NotFound();
            var result = await walletService.GetSpecialistTransactionsAsync(specialistId.Value, page, pageSize);
            if (result.IsError) return result.Errors.ToProblem();
            return Results.Ok(ApiResponse<List<TransactionDto>>.SuccessResponse(result.Value));
        }

        if (role == nameof(UserRole.Admin))
        {
            var result = await walletService.GetAdminTransactionsAsync(currentUser.Id.Value, page, pageSize);
            if (result.IsError) return result.Errors.ToProblem();
            return Results.Ok(ApiResponse<List<TransactionDto>>.SuccessResponse(result.Value));
        }

        {
            var result = await walletService.GetUserTransactionsAsync(currentUser.Id.Value, page, pageSize);
            if (result.IsError) return result.Errors.ToProblem();
            return Results.Ok(ApiResponse<List<TransactionDto>>.SuccessResponse(result.Value));
        }
    }
}
