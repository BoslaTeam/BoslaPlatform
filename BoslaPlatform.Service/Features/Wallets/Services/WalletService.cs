using BoslaPlatform.Application.Features.Wallets.DTOs;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Wallets;
using BoslaPlatform.Domain.Entities.Payouts;
using BoslaPlatform.Shared;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.Features.Wallets.Services;

public class WalletService(IAppDbContext context) : IWalletService
{
    public async Task<Result<WalletResponseDto>> GetSpecialistWalletAsync(Guid specialistId)
    {
        var wallet = await context.Set<SpecialistWallet>()
            .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAtUtc).Take(20))
            .FirstOrDefaultAsync(w => w.OwnerId == specialistId);

        if (wallet is null)
        {
            wallet = new SpecialistWallet(specialistId);
            context.Set<SpecialistWallet>().Add(wallet);
            await context.SaveChangesAsync();
        }

        return Result<WalletResponseDto>.Success(MapToDto(wallet));
    }

    public async Task<Result<List<TransactionDto>>> GetSpecialistTransactionsAsync(Guid specialistId, int page = 1, int pageSize = 20)
    {
        var wallet = await context.Set<SpecialistWallet>()
            .FirstOrDefaultAsync(w => w.OwnerId == specialistId);

        if (wallet is null) return Result<List<TransactionDto>>.Success([]);

        var transactions = await context.Set<WalletTransaction>()
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Result<List<TransactionDto>>.Success(transactions.Select(MapTransaction).ToList());
    }

    public async Task<Result<WalletResponseDto>> GetUserWalletAsync(Guid userId)
    {
        var wallet = await context.Set<UserWallet>()
            .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAtUtc).Take(20))
            .FirstOrDefaultAsync(w => w.OwnerId == userId);

        if (wallet is null)
        {
            wallet = new UserWallet(userId);
            context.Set<UserWallet>().Add(wallet);
            await context.SaveChangesAsync();
        }

        return Result<WalletResponseDto>.Success(MapToDto(wallet));
    }

    public async Task<Result<List<TransactionDto>>> GetUserTransactionsAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        var wallet = await context.Set<UserWallet>()
            .FirstOrDefaultAsync(w => w.OwnerId == userId);

        if (wallet is null) return Result<List<TransactionDto>>.Success([]);

        var transactions = await context.Set<WalletTransaction>()
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Result<List<TransactionDto>>.Success(transactions.Select(MapTransaction).ToList());
    }

    public async Task<Result<AdminWalletStatsDto>> GetAdminWalletStatsAsync()
    {
        if (context is not DbContext dbContext)
            return Error.Unexpected("Database.ConnectionError", "Could not establish a database connection.");

        var connection = dbContext.Database.GetDbConnection();

        const string sql = @"
            SELECT
                ISNULL(SUM(p.PlatformFeeAmount), 0) AS TotalPlatformFees,
                ISNULL(SUM(p.TaxAmount), 0) AS TotalTaxes,
                ISNULL(SUM(p.SpecialistAmount), 0) AS TotalPaidToSpecialists,
                ISNULL(SUM(CASE WHEN p.Status = 'Refunded' THEN p.Amount ELSE 0 END), 0) AS TotalRefunded,
                COUNT(CASE WHEN p.Status = 'Completed' THEN 1 END) AS TotalCompletedPayments,
                COUNT(CASE WHEN p.Status = 'Refunded' THEN 1 END) AS TotalRefundedPayments
            FROM Payments p;";

        var stats = await connection.QuerySingleAsync<AdminWalletStatsDto>(sql);
        stats.AvailableBalance = stats.TotalPlatformFees + stats.TotalTaxes - stats.TotalRefunded;
        return Result<AdminWalletStatsDto>.Success(stats);
    }

    public async Task<Result<WalletResponseDto>> GetAdminWalletAsync(Guid adminId)
    {
        var wallet = await context.Set<PlatformWallet>()
            .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAtUtc).Take(20))
            .FirstOrDefaultAsync(w => w.OwnerId == adminId);

        if (wallet is null)
        {
            wallet = new PlatformWallet(adminId);
            context.Set<PlatformWallet>().Add(wallet);
            await context.SaveChangesAsync();
        }

        return Result<WalletResponseDto>.Success(MapToDto(wallet));
    }

    public async Task<Result<List<TransactionDto>>> GetAdminTransactionsAsync(Guid adminId, int page = 1, int pageSize = 20)
    {
        var wallet = await context.Set<PlatformWallet>()
            .FirstOrDefaultAsync(w => w.OwnerId == adminId);

        if (wallet is null) return Result<List<TransactionDto>>.Success([]);

        var transactions = await context.Set<WalletTransaction>()
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Result<List<TransactionDto>>.Success(transactions.Select(MapTransaction).ToList());
    }

    public async Task<Result<List<TransactionDto>>> GetAllTransactionsAsync(int page = 1, int pageSize = 50)
    {
        var transactions = await context.Set<WalletTransaction>()
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Result<List<TransactionDto>>.Success(transactions.Select(MapTransaction).ToList());
    }

    private static WalletResponseDto MapToDto(Wallet wallet)
    {
        return new WalletResponseDto
        {
            Id = wallet.Id,
            Balance = wallet.Balance,
            HoldBalance = wallet.HoldBalance,
            Currency = wallet.Currency,
            RecentTransactions = wallet.Transactions.OrderByDescending(t => t.CreatedAtUtc).Take(20).Select(MapTransaction).ToList()
        };
    }

    private static TransactionDto MapTransaction(WalletTransaction t)
    {
        return new TransactionDto
        {
            Id = t.Id,
            Amount = t.Amount,
            Type = t.Type.ToString(),
            Description = t.Description,
            ReferenceType = t.ReferenceType,
            ReferenceId = t.ReferenceId,
            CreatedAtUtc = t.CreatedAtUtc
        };
    }
}
