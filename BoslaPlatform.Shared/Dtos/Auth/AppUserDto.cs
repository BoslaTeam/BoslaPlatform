namespace BoslaPlatform.Shared.Dtos.Auth
{
    public sealed record AppUserDto(string UserId, string Email, IList<string> Roles, IList<ClaimDto> Claims);

}
