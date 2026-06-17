using System.Threading.Tasks;

namespace BoslaPlatform.Application.Interfaces.Communication
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
    }
}
