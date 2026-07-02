using BoslaPlatform.Application.Features.Withdrawals.DTOs;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces;

public interface IWithdrawalService
{
    Task<Result<WalletDto>> GetWalletAsync(Guid specialistId);
    Task<Result<WithdrawalDto>> RequestWithdrawalAsync(Guid specialistId, WithdrawRequestDto request);
    Task<Result<List<WithdrawalDto>>> GetHistoryAsync(Guid specialistId);

    // Admin
    Task<Result<List<WithdrawalListDto>>> GetPendingWithdrawalsAsync();
    Task<Result<List<WithdrawalListDto>>> GetAllWithdrawalsAsync(string? status = null);
    Task<Result<WithdrawalDetailDto>> GetWithdrawalDetailAsync(Guid withdrawalId);
    Task<Result> ApproveWithdrawalAsync(Guid withdrawalId, Guid adminId, string? notes = null);
    Task<Result> RejectWithdrawalAsync(Guid withdrawalId, Guid adminId, string? notes = null);
    Task<Result> MarkCompletedAsync(Guid withdrawalId);
}
