using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Favorites.DTOs;
using BoslaPlatform.Application.Features.Favorites.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BoslaPlatform.API.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/favorites")]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;

        public FavoritesController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        [HttpGet("specialists")]
        [ProducesResponseType(typeof(ApiResponse<List<FavoriteSpecialistDto>>), StatusCodes.Status200OK)]
        public async Task<IResult> GetMyFavorites(CancellationToken ct)
        {
            var result = await _favoriteService.GetMyFavoritesAsync(ct);
            return result.Match(
                value => Results.Ok(ApiResponse<List<FavoriteSpecialistDto>>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpPost("specialists/{specialistId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IResult> ToggleFavorite(Guid specialistId, CancellationToken ct)
        {
            var result = await _favoriteService.ToggleFavoriteAsync(specialistId, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpGet("specialists/{specialistId:guid}/status")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IResult> IsFavorited(Guid specialistId, CancellationToken ct)
        {
            var result = await _favoriteService.IsFavoritedAsync(specialistId, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }
    }
}
