namespace BoslaPlatform.Application.Features.Specialists.Request
{
    public sealed class AddAvailabilitiesRequest
    {
        public IReadOnlyList<AvailabilityItemRequest> Availabilities { get; init; } = [];
    }
}
