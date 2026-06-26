namespace BoslaPlatform.Application.Features.Specialists.Request
{
    public sealed record AvailabilityItemRequest(
        DateTimeOffset Start,
        DateTimeOffset End);
}
