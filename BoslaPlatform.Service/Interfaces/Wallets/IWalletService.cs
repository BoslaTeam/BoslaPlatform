using BoslaPlatform.Application.Features.Wallets.DTOs;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Wallets;

public interface IWalletService
{
    // Specialist wallet
    Task<Result<WalletResponseDto>> GetSpecialistWalletAsync(Guid specialistId);
    Task<Result<List<TransactionDto>>> GetSpecialistTransactionsAsync(Guid specialistId, int page = 1, int pageSize = 20);

    // User wallet
    Task<Result<WalletResponseDto>> GetUserWalletAsync(Guid userId);
    Task<Result<List<TransactionDto>>> GetUserTransactionsAsync(Guid userId, int page = 1, int pageSize = 20);

    // Admin wallet
    Task<Result<AdminWalletStatsDto>> GetAdminWalletStatsAsync();
    Task<Result<WalletResponseDto>> GetAdminWalletAsync(Guid adminId);
    Task<Result<List<TransactionDto>>> GetAdminTransactionsAsync(Guid adminId, int page = 1, int pageSize = 20);
    Task<Result<List<TransactionDto>>> GetAllTransactionsAsync(int page = 1, int pageSize = 50);
}
