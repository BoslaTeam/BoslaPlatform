namespace BoslaPlatform.Application.Features.Admin.Requests;

public sealed class CancelAppointmentRequest
{
    public string Reason { get; set; } = string.Empty;
}
