namespace BoslaPlatform.Application.Features.Specialists.Request
{
    public sealed record AddAvailabilityRequest(
    DateTimeOffset Start,
    DateTimeOffset End); 
}
