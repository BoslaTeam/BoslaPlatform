using BoslaPlatform.Application.Features.Withdrawals.DTOs;
using BoslaPlatform.Application.Interfaces;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Entities.Payouts;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Shared;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace BoslaPlatform.Application.Features.Withdrawals.Services;

public class WithdrawalService(
    IAppDbContext context) : IWithdrawalService
{
    private async Task<SpecialistWallet> GetOrCreateWalletAsync(Guid specialistId)
    {
        var wallet = await context.Set<SpecialistWallet>()
            .FirstOrDefaultAsync(w => w.SpecialistId == specialistId);

        if (wallet is null)
        {
            wallet = new SpecialistWallet(specialistId);
            context.Set<SpecialistWallet>().Add(wallet);
            await context.SaveChangesAsync();
        }

        return wallet;
    }

    public async Task<Result<WalletDto>> GetWalletAsync(Guid specialistId)
    {
        if (context is not DbContext dbContext)
            return Error.Unexpected("Database.ConnectionError", "Could not establish a database connection.");

        var connection = dbContext.Database.GetDbConnection();

        const string sql = @"
            SELECT 
                ISNULL(SUM(CASE WHEN p.EscrowStatus NOT IN ('Refunded', 'Disputed') THEN p.SpecialistAmount ELSE 0 END), 0) AS TotalEarnings,
                ISNULL(SUM(CASE WHEN p.EscrowStatus = 'Released' THEN p.SpecialistAmount ELSE 0 END), 0) AS ReleasedEarnings,
                ISNULL(SUM(CASE WHEN p.EscrowStatus = 'Held' THEN p.SpecialistAmount ELSE 0 END), 0) AS HeldEarnings,
                MIN(CASE WHEN p.EscrowStatus = 'Held' THEN p.HeldUntil ELSE NULL END) AS NextReleaseDate
            FROM Payments p
            INNER JOIN Appointments a ON p.AppointmentId = a.Id
            WHERE a.SpecialistId = @Id;

            SELECT ISNULL(SUM(w.Amount), 0) AS TotalWithdrawn
            FROM Withdrawals w
            WHERE w.SpecialistId = @Id AND w.Status = 'Completed';

            SELECT TOP 5
                w.Id, w.Amount, w.Status, w.PaymentMethod, w.PaymentDetails, w.CreatedAtUtc AS RequestedAt, w.ProcessedAt, w.AdminNotes
            FROM Withdrawals w
            WHERE w.SpecialistId = @Id
            ORDER BY w.CreatedAtUtc DESC;";

        using var multi = await connection.QueryMultipleAsync(sql, new { Id = specialistId });

        var summary = await multi.ReadSingleAsync<dynamic>();
        var totalWithdrawn = await multi.ReadSingleAsync<dynamic>();
        var recent = (await multi.ReadAsync<WithdrawalDto>()).ToList();

        var releasedEarnings = (decimal)summary.ReleasedEarnings;
        var withdrawn = (decimal)totalWithdrawn.TotalWithdrawn;
        DateTime? nextReleaseDate = summary.NextReleaseDate;

        return new WalletDto
        {
            TotalEarnings = (decimal)summary.TotalEarnings,
            AvailableBalance = releasedEarnings - withdrawn,
            PendingBalance = 0,
            PendingReleaseBalance = (decimal)summary.HeldEarnings,
            NextReleaseDate = nextReleaseDate,
            TotalWithdrawn = withdrawn,
            RecentWithdrawals = recent
        };
    }

    public async Task<Result<WithdrawalDto>> RequestWithdrawalAsync(Guid specialistId, WithdrawRequestDto request)
    {
        var specialist = await context.Specialists
            .Include(s => s.Verification)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == specialistId);

        if (specialist is null)
            return Error.NotFound("Specialist.NotFound", "Specialist not found.");

        if (specialist.Verification == null || specialist.Verification.Status != VerificationStatus.Approved)
            return Error.Failure("Specialist.NotVerified", "Your account must be verified before withdrawing.");

        if (request.Amount <= 0)
            return Error.Validation("Amount.Invalid", "Amount must be greater than zero.");

        var walletDto = await GetWalletAsync(specialistId);
        if (walletDto.IsError)
            return Error.Unexpected("Wallet.Error", "Could not load wallet.");

        if (request.Amount > walletDto.Value.AvailableBalance)
            return Error.Validation("Amount.Insufficient",
                $"Insufficient balance. Available: {walletDto.Value.AvailableBalance:C}");

        var withdrawal = Withdrawal.RequestDirect(specialistId, request.Amount, request.PaymentMethod, request.PaymentDetails);

        context.Set<Withdrawal>().Add(withdrawal);
        await context.SaveChangesAsync();

        var dto = new WithdrawalDto
        {
            Id = withdrawal.Id,
            Amount = withdrawal.Amount,
            Status = withdrawal.Status.ToString(),
            PaymentMethod = withdrawal.PaymentMethod,
            PaymentDetails = withdrawal.PaymentDetails,
            RequestedAt = withdrawal.CreatedAtUtc,
            ProcessedAt = withdrawal.ProcessedAt,
            AdminNotes = withdrawal.AdminNotes
        };

        return dto;
    }

    public async Task<Result<List<WithdrawalDto>>> GetHistoryAsync(Guid specialistId)
    {
        var items = await context.Set<Withdrawal>()
            .Where(w => w.SpecialistId == specialistId)
            .OrderByDescending(w => w.CreatedAtUtc)
            .Select(w => new WithdrawalDto
            {
                Id = w.Id,
                Amount = w.Amount,
                Status = w.Status.ToString(),
                PaymentMethod = w.PaymentMethod,
                PaymentDetails = w.PaymentDetails,
                RequestedAt = w.CreatedAtUtc,
                ProcessedAt = w.ProcessedAt,
                AdminNotes = w.AdminNotes
            })
            .ToListAsync();

        return items;
    }

    // ---- Admin methods ----

    public async Task<Result<List<WithdrawalListDto>>> GetPendingWithdrawalsAsync()
    {
        var items = await context.Set<Withdrawal>()
            .Include(w => w.Specialist)
            .ThenInclude(s => s.User)
            .Where(w => w.Status == WithdrawalStatus.Pending)
            .OrderBy(w => w.CreatedAtUtc)
            .Select(w => new WithdrawalListDto
            {
                Id = w.Id,
                Amount = w.Amount,
                Status = w.Status.ToString(),
                PaymentMethod = w.PaymentMethod,
                RequestedAt = w.CreatedAtUtc,
                ProcessedAt = w.ProcessedAt,
                SpecialistId = w.SpecialistId,
                SpecialistName = w.Specialist.User.Name,
                SpecialistImage = w.Specialist.User.ProfileImageUrl,
                SpecialistTitle = w.Specialist.User.Title
            })
            .ToListAsync();

        return items;
    }

    public async Task<Result<List<WithdrawalListDto>>> GetAllWithdrawalsAsync(string? status = null)
    {
        var query = context.Set<Withdrawal>()
            .Include(w => w.Specialist)
            .ThenInclude(s => s.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<WithdrawalStatus>(status, true, out var statusEnum))
            query = query.Where(w => w.Status == statusEnum);

        var items = await query
            .OrderByDescending(w => w.CreatedAtUtc)
            .Select(w => new WithdrawalListDto
            {
                Id = w.Id,
                Amount = w.Amount,
                Status = w.Status.ToString(),
                PaymentMethod = w.PaymentMethod,
                RequestedAt = w.CreatedAtUtc,
                ProcessedAt = w.ProcessedAt,
                SpecialistId = w.SpecialistId,
                SpecialistName = w.Specialist.User.Name,
                SpecialistImage = w.Specialist.User.ProfileImageUrl,
                SpecialistTitle = w.Specialist.User.Title
            })
            .ToListAsync();

        return items;
    }

    public async Task<Result<WithdrawalDetailDto>> GetWithdrawalDetailAsync(Guid withdrawalId)
    {
        var withdrawal = await context.Set<Withdrawal>()
            .Include(w => w.Specialist)
            .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(w => w.Id == withdrawalId);

        if (withdrawal is null)
            return Error.NotFound("Withdrawal.NotFound", "Withdrawal not found.");

        return new WithdrawalDetailDto
        {
            Id = withdrawal.Id,
            Amount = withdrawal.Amount,
            Status = withdrawal.Status.ToString(),
            PaymentMethod = withdrawal.PaymentMethod,
            PaymentDetails = withdrawal.PaymentDetails,
            RequestedAt = withdrawal.CreatedAtUtc,
            ProcessedAt = withdrawal.ProcessedAt,
            ReviewedBy = withdrawal.ReviewedBy,
            AdminNotes = withdrawal.AdminNotes,
            SpecialistId = withdrawal.SpecialistId,
            SpecialistName = withdrawal.Specialist.User.Name,
            SpecialistAvatar = withdrawal.Specialist.User.ProfileImageUrl
        };
    }

    public async Task<Result> ApproveWithdrawalAsync(Guid withdrawalId, Guid adminId, string? notes = null)
    {
        var withdrawal = await context.Set<Withdrawal>()
            .FirstOrDefaultAsync(w => w.Id == withdrawalId);

        if (withdrawal is null)
            return Error.NotFound("Withdrawal.NotFound", "Withdrawal not found.");

        if (withdrawal.Status != WithdrawalStatus.Pending)
            return Error.Conflict("Withdrawal.NotPending",
                $"Cannot approve withdrawal in status '{withdrawal.Status}'.");

        withdrawal.Approve(adminId);
        if (!string.IsNullOrEmpty(notes))
            withdrawal.AdminNotes = notes;

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> RejectWithdrawalAsync(Guid withdrawalId, Guid adminId, string? notes = null)
    {
        var withdrawal = await context.Set<Withdrawal>()
            .FirstOrDefaultAsync(w => w.Id == withdrawalId);

        if (withdrawal is null)
            return Error.NotFound("Withdrawal.NotFound", "Withdrawal not found.");

        if (withdrawal.Status != WithdrawalStatus.Pending)
            return Error.Conflict("Withdrawal.NotPending",
                $"Cannot reject withdrawal in status '{withdrawal.Status}'.");

        withdrawal.Reject(adminId, notes);

        var wallet = await GetOrCreateWalletAsync(withdrawal.SpecialistId);
        wallet.ReleaseHold(withdrawal.Amount, $"إلغاء السحب {withdrawal.Amount:C} — رد المبلغ", "Withdrawal", withdrawal.Id);

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> MarkCompletedAsync(Guid withdrawalId)
    {
        var withdrawal = await context.Set<Withdrawal>()
            .FirstOrDefaultAsync(w => w.Id == withdrawalId);

        if (withdrawal is null)
            return Error.NotFound("Withdrawal.NotFound", "Withdrawal not found.");

        if (withdrawal.Status != WithdrawalStatus.Processing)
            return Error.Conflict("Withdrawal.NotProcessing",
                $"Cannot complete withdrawal in status '{withdrawal.Status}'.");

        withdrawal.Complete();

        var wallet = await GetOrCreateWalletAsync(withdrawal.SpecialistId);
        wallet.ReleaseHoldAndDebit(withdrawal.Amount, $"اكتمال السحب {withdrawal.Amount:C}", "Withdrawal", withdrawal.Id);

        await context.SaveChangesAsync();
        return Result.Success();
    }
}
