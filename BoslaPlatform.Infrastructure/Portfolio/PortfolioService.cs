using BoslaPlatform.Application.Features.Portfolio.DTOs;
using BoslaPlatform.Application.Features.Portfolio.Requests;
using BoslaPlatform.Application.Features.Portfolio.Services;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Specialists;
using BoslaPlatform.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BoslaPlatform.Domain.Entities;

namespace BoslaPlatform.Infrastructure.Portfolio
{
    public class PortfolioService : IPortfolioService
    {
        private readonly IAppDbContext _context;
        private readonly IUser _currentUser;
        private readonly IPublisher _publisher;

        public PortfolioService(IAppDbContext context, IUser currentUser, IPublisher publisher)
        {
            _context = context;
            _currentUser = currentUser;
            _publisher = publisher;
        }

        private Result<Guid> GetUserId()
        {
            if (_currentUser.Id == null)
                return Error.Unauthorized("User.Unauthorized", "User is not authenticated.");
            return _currentUser.Id.Value;
        }

        private async Task<Result<Guid>> GetSpecialistId(CancellationToken ct)
        {
            var userIdResult = GetUserId();
            if (userIdResult.IsError)
                return userIdResult.Errors;

            var specialist = await _context.Set<Specialist>()
                .FirstOrDefaultAsync(s => s.UserId == userIdResult.Value, ct);

            if (specialist == null)
                return Error.NotFound("Specialist.NotFound", "Specialist profile not found.");

            return specialist.Id;
        }

        private static PortfolioItemDto ToDto(SpecialistPortfolioItem item) => new(
            item.Id, item.Title, item.Description, item.CoverImageUrl, item.WorkUrl,
            item.Status.ToString(), item.AdminNotes, item.SortOrder, item.CreatedAtUtc,
            item.Images.OrderBy(i => i.SortOrder).Select(i => new PortfolioItemImageDto(i.Id, i.ImageUrl, i.SortOrder)).ToList());

        private async Task ReplaceImagesAsync(SpecialistPortfolioItem item, List<string> imageUrls, CancellationToken ct)
        {
            var existing = await _context.Set<PortfolioItemImage>()
                .Where(i => i.PortfolioItemId == item.Id)
                .ToListAsync(ct);
            _context.Set<PortfolioItemImage>().RemoveRange(existing);

            foreach (var (url, idx) in imageUrls.Select((url, idx) => (url, idx)))
            {
                _context.Set<PortfolioItemImage>().Add(new PortfolioItemImage
                {
                    PortfolioItemId = item.Id,
                    ImageUrl = url,
                    SortOrder = idx,
                });
            }
        }

        public async Task<Result<List<PortfolioItemDto>>> GetMyAsync(CancellationToken ct = default)
        {
            var specResult = await GetSpecialistId(ct);
            if (specResult.IsError)
                return specResult.Errors;

            var items = await _context.Set<SpecialistPortfolioItem>()
                .Include(p => p.Images)
                .Where(p => p.SpecialistId == specResult.Value)
                .OrderBy(p => p.SortOrder)
                .ThenByDescending(p => p.CreatedAtUtc)
                .ToListAsync(ct);

            return Result<List<PortfolioItemDto>>.Success(items.Select(ToDto).ToList());
        }

        public async Task<Result<PortfolioItemDto>> CreateAsync(CreatePortfolioItemRequest request, CancellationToken ct = default)
        {
            var specResult = await GetSpecialistId(ct);
            if (specResult.IsError)
                return specResult.Errors;

            var maxOrder = await _context.Set<SpecialistPortfolioItem>()
                .Where(p => p.SpecialistId == specResult.Value)
                .MaxAsync(p => (int?)p.SortOrder, ct) ?? 0;

            var item = new SpecialistPortfolioItem
            {
                SpecialistId = specResult.Value,
                Title = request.Title,
                Description = request.Description,
                CoverImageUrl = request.CoverImageUrl,
                WorkUrl = request.WorkUrl,
                Status = PortfolioItemStatus.Pending,
                SortOrder = maxOrder + 1,
            };

            _context.Set<SpecialistPortfolioItem>().Add(item);
            await _context.SaveChangesAsync(ct);

            if (request.ImageUrls.Count > 0)
                await ReplaceImagesAsync(item, request.ImageUrls, ct);

            await _context.SaveChangesAsync(ct);
            await _publisher.Publish(new PortfolioItemSubmittedEvent(specResult.Value, item.Id, item.Title), ct);

            // Reload with images
            item = await _context.Set<SpecialistPortfolioItem>()
                .Include(p => p.Images)
                .FirstAsync(p => p.Id == item.Id, ct);

            return Result<PortfolioItemDto>.Success(ToDto(item));
        }

        public async Task<Result<PortfolioItemDto>> UpdateAsync(Guid id, UpdatePortfolioItemRequest request, CancellationToken ct = default)
        {
            var specResult = await GetSpecialistId(ct);
            if (specResult.IsError)
                return specResult.Errors;

            var item = await _context.Set<SpecialistPortfolioItem>()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id && p.SpecialistId == specResult.Value, ct);

            if (item == null)
                return Error.NotFound("Portfolio.NotFound", "Portfolio item not found.");

            var wasApproved = item.Status == PortfolioItemStatus.Approved;

            item.Title = request.Title;
            item.Description = request.Description;
            item.CoverImageUrl = request.CoverImageUrl;
            item.WorkUrl = request.WorkUrl;
            if (wasApproved || item.Status == PortfolioItemStatus.Rejected)
                item.Status = PortfolioItemStatus.Pending;

            if (request.ImageUrls.Count > 0)
                await ReplaceImagesAsync(item, request.ImageUrls, ct);

            await _context.SaveChangesAsync(ct);

            if (wasApproved)
                await _publisher.Publish(new PortfolioItemSubmittedEvent(specResult.Value, item.Id, item.Title), ct);

            return Result<PortfolioItemDto>.Success(ToDto(item));
        }

        public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var specResult = await GetSpecialistId(ct);
            if (specResult.IsError)
                return specResult.Errors;

            var item = await _context.Set<SpecialistPortfolioItem>()
                .FirstOrDefaultAsync(p => p.Id == id && p.SpecialistId == specResult.Value, ct);

            if (item == null)
                return Error.NotFound("Portfolio.NotFound", "Portfolio item not found.");

            _context.Set<SpecialistPortfolioItem>().Remove(item);
            await _context.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> ReorderAsync(ReorderPortfolioRequest request, CancellationToken ct = default)
        {
            var specResult = await GetSpecialistId(ct);
            if (specResult.IsError)
                return specResult.Errors;

            var ids = request.Items.Select(i => i.Id).ToList();
            var items = await _context.Set<SpecialistPortfolioItem>()
                .Where(p => p.SpecialistId == specResult.Value && ids.Contains(p.Id))
                .ToListAsync(ct);

            foreach (var req in request.Items)
            {
                var item = items.FirstOrDefault(p => p.Id == req.Id);
                if (item != null)
                    item.SortOrder = req.SortOrder;
            }

            await _context.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }

        public async Task<Result<PortfolioItemDto>> GetByIdAsync(Guid specialistId, Guid itemId, CancellationToken ct = default)
        {
            var item = await _context.Set<SpecialistPortfolioItem>()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == itemId && p.SpecialistId == specialistId && p.Status == PortfolioItemStatus.Approved, ct);

            if (item == null)
                return Error.NotFound("Portfolio.NotFound", "Portfolio item not found.");

            return Result<PortfolioItemDto>.Success(ToDto(item));
        }

        public async Task<Result<List<PortfolioItemDto>>> GetPublicAsync(Guid specialistId, CancellationToken ct = default)
        {
            var items = await _context.Set<SpecialistPortfolioItem>()
                .Include(p => p.Images)
                .Where(p => p.SpecialistId == specialistId && p.Status == PortfolioItemStatus.Approved)
                .OrderBy(p => p.SortOrder)
                .ThenByDescending(p => p.CreatedAtUtc)
                .ToListAsync(ct);

            return Result<List<PortfolioItemDto>>.Success(items.Select(ToDto).ToList());
        }

        public async Task<Result<List<PortfolioItemDto>>> GetAllBySpecialistAsync(Guid specialistId, CancellationToken ct = default)
        {
            var items = await _context.Set<SpecialistPortfolioItem>()
                .Include(p => p.Images)
                .Where(p => p.SpecialistId == specialistId)
                .OrderBy(p => p.SortOrder)
                .ThenByDescending(p => p.CreatedAtUtc)
                .ToListAsync(ct);

            return Result<List<PortfolioItemDto>>.Success(items.Select(ToDto).ToList());
        }

        public async Task<Result<PortfolioItemDto>> ApproveAsync(Guid specialistId, Guid itemId, AdminReviewPortfolioRequest request, CancellationToken ct = default)
        {
            var item = await _context.Set<SpecialistPortfolioItem>()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == itemId && p.SpecialistId == specialistId, ct);

            if (item == null)
                return Error.NotFound("Portfolio.NotFound", "Portfolio item not found.");

            item.Status = PortfolioItemStatus.Approved;
            item.AdminNotes = request.AdminNotes;
            await _context.SaveChangesAsync(ct);

            await _publisher.Publish(new PortfolioItemApprovedEvent(specialistId, item.Id, item.Title), ct);

            return Result<PortfolioItemDto>.Success(ToDto(item));
        }

        public async Task<Result<PortfolioItemDto>> RejectAsync(Guid specialistId, Guid itemId, AdminReviewPortfolioRequest request, CancellationToken ct = default)
        {
            var item = await _context.Set<SpecialistPortfolioItem>()
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == itemId && p.SpecialistId == specialistId, ct);

            if (item == null)
                return Error.NotFound("Portfolio.NotFound", "Portfolio item not found.");

            item.Status = PortfolioItemStatus.Rejected;
            item.AdminNotes = request.AdminNotes;
            await _context.SaveChangesAsync(ct);

            await _publisher.Publish(new PortfolioItemRejectedEvent(specialistId, item.Id, item.Title, request.AdminNotes), ct);

            return Result<PortfolioItemDto>.Success(ToDto(item));
        }
    }
}
