using BoslaPlatform.Shared;
using BoslaPlatform.Application.Features.Users.DTOs;
using BoslaPlatform.Application.Features.Users.Requests;

namespace BoslaPlatform.Application.Features.Users.Services
{
    public interface IUserService
    {
        Task<Result<UserProfileDto>> GetMyProfileAsync(CancellationToken ct = default);
        Task<Result<UserProfileDto>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
        Task<Result<bool>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
        
        Task<Result<List<EducationDto>>> GetEducationAsync(CancellationToken ct = default);
        Task<Result<EducationDto>> AddEducationAsync(AddEducationRequest request, CancellationToken ct = default);
        Task<Result<EducationDto>> UpdateEducationAsync(Guid id, UpdateEducationRequest request, CancellationToken ct = default);
        Task<Result<bool>> DeleteEducationAsync(Guid id, CancellationToken ct = default);

        Task<Result<List<SocialLinkDto>>> GetSocialLinksAsync(CancellationToken ct = default);
        Task<Result<SocialLinkDto>> AddSocialLinkAsync(AddSocialLinkRequest request, CancellationToken ct = default);
        Task<Result<bool>> DeleteSocialLinkAsync(Guid id, CancellationToken ct = default);

        Task<Result<UserProfileDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    }
}
