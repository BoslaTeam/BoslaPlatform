namespace BoslaPlatform.Service.Features.AI.Requests;

public class FeedbackRequest
{
    public bool WasHelpful { get; set; }
    public Guid? ClickedSpecialistId { get; set; }
}
