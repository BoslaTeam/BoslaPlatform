namespace BoslaPlatform.Application.Features.Notifications.DTOs
{
    public sealed record NotificationPreferenceDto(
        string Type,
        bool Enabled);
}
