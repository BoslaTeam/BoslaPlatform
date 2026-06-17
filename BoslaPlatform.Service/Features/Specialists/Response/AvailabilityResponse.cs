namespace BoslaPlatform.Application.Features.Specialists.Response
{
    public sealed record AvailabilityResponse(
    Guid Id,
    DateTimeOffset Start,
    DateTimeOffset End);
}
