using BoslaPlatform.Application.Features.Portfolio.DTOs;
using BoslaPlatform.Application.Features.Portfolio.Requests;
using BoslaPlatform.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BoslaPlatform.Application.Features.Portfolio.Services
{
    public interface IPortfolioService
    {
        // Specialist's own portfolio
        Task<Result<List<PortfolioItemDto>>> GetMyAsync(CancellationToken ct = default);
        Task<Result<PortfolioItemDto>> CreateAsync(CreatePortfolioItemRequest request, CancellationToken ct = default);
        Task<Result<PortfolioItemDto>> UpdateAsync(Guid id, UpdatePortfolioItemRequest request, CancellationToken ct = default);
        Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<Result<bool>> ReorderAsync(ReorderPortfolioRequest request, CancellationToken ct = default);

        // Public
        Task<Result<List<PortfolioItemDto>>> GetPublicAsync(Guid specialistId, CancellationToken ct = default);
        Task<Result<PortfolioItemDto>> GetByIdAsync(Guid specialistId, Guid itemId, CancellationToken ct = default);

        // Admin
        Task<Result<List<PortfolioItemDto>>> GetAllBySpecialistAsync(Guid specialistId, CancellationToken ct = default);
        Task<Result<PortfolioItemDto>> ApproveAsync(Guid specialistId, Guid itemId, AdminReviewPortfolioRequest request, CancellationToken ct = default);
        Task<Result<PortfolioItemDto>> RejectAsync(Guid specialistId, Guid itemId, AdminReviewPortfolioRequest request, CancellationToken ct = default);
    }
}
