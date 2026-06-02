namespace BoslaPlatform.Application
{
    public sealed record AppUserDto(string UserId, string Email, IList<string> Roles, IList<ClaimDto> Claims);

}
