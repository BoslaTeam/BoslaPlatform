using BoslaPlatform.Application.Features.Favorites.DTOs;
using BoslaPlatform.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BoslaPlatform.Application.Features.Favorites.Services
{
    public interface IFavoriteService
    {
        Task<Result<List<FavoriteSpecialistDto>>> GetMyFavoritesAsync(CancellationToken ct = default);
        Task<Result<bool>> ToggleFavoriteAsync(Guid specialistId, CancellationToken ct = default);
        Task<Result<bool>> IsFavoritedAsync(Guid specialistId, CancellationToken ct = default);
    }
}
