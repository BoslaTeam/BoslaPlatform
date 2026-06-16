namespace BoslaPlatform.Application.Features.Specialists.Request
{
    public sealed record AddAvailabilityRequest(
        string Day,       
        string StartTime, 
        string EndTime    
    );
}
