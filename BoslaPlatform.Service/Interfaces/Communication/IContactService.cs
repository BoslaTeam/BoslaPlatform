using BoslaPlatform.Application.Features.Contact.Requests;

namespace BoslaPlatform.Application.Interfaces.Communication
{
    public interface IContactService
    {
        Task HandleContactAsync(ContactRequest request, CancellationToken ct = default);
    }
}
