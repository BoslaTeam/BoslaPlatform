namespace BoslaPlatform.Application.Features.Specialists.Response
{
    public sealed record AvailabilityResponse(
        Guid Id,
        string Day,       
        string StartTime, 
        string EndTime    
    );
}
