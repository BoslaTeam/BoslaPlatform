using BoslaPlatform.Application.Features.Contact.Requests;
using BoslaPlatform.Application.Interfaces.Communication;
using BoslaPlatform.Application.Settings;
using Microsoft.Extensions.Options;

namespace BoslaPlatform.Infrastructure.Communication
{
    public sealed class ContactService : IContactService
    {
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;

        private static readonly Dictionary<string, string> SubjectLabels = new()
        {
            ["general"] = "استفسار عام",
            ["support"] = "دعم فني",
            ["partnership"] = "شراكة",
            ["specialist"] = "انضمام كمتخصص",
            ["complaint"] = "شكوى",
            ["other"] = "أخرى",
        };

        public ContactService(IEmailService emailService, IOptionsSnapshot<EmailSettings> emailSettings)
        {
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
        }

        public async Task HandleContactAsync(ContactRequest request, CancellationToken ct = default)
        {
            var subjectLabel = SubjectLabels.GetValueOrDefault(request.Subject, request.Subject);
            var subject = $"رسالة جديدة من {request.Name} — {subjectLabel}";

            var body = BuildEmailBody(request, subjectLabel);

            await _emailService.SendEmailAsync(_emailSettings.FromEmail, subject, body);
        }

        private static string BuildEmailBody(ContactRequest request, string subjectLabel)
        {
            var safeName = HtmlEncode(request.Name);
            var safeEmail = HtmlEncode(request.Email);
            var safeSubject = HtmlEncode(subjectLabel);
            var safeMessage = HtmlEncode(request.Message);

            return $@"<!DOCTYPE html>
<html lang=""ar"" dir=""rtl"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>رسالة جديدة</title>
</head>
<body style=""margin:0;padding:0;background-color:#F7F9FA;font-family:'Cairo','Inter',Tahoma,sans-serif;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;padding:30px 10px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""600"" max-width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 12px rgba(0,0,0,0.06);"">

          
          <tr>
            <td style=""background:linear-gradient(135deg,#1B4F72 0%,#2E86AB 100%);padding:30px 40px;text-align:center;"">
              <div style=""display:inline-block;position:relative;width:48px;height:48px;margin-bottom:8px;"">
                <svg width=""48"" height=""48"" viewBox=""0 0 48 48"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" style=""display:block;margin:0 auto;"">
                  <circle cx=""24"" cy=""24"" r=""22"" fill=""#1B4F72"" stroke=""#ffffff"" stroke-width=""2""/>
                  <polygon points=""24,6 28,22 24,18 20,22"" fill=""#F39C12""/>
                  <polygon points=""24,42 20,26 24,30 28,26"" fill=""#ffffff""/>
                  <circle cx=""24"" cy=""24"" r=""4"" fill=""#ffffff""/>
                </svg>
              </div>
              <h1 style=""color:#ffffff;font-size:24px;font-weight:700;margin:8px 0 0 0;letter-spacing:0.5px;"">بوصلة</h1>
              <p style=""color:rgba(255,255,255,0.8);font-size:14px;margin:4px 0 0 0;"">Your Compass to the Right Expert</p>
            </td>
          </tr>

          <tr>
            <td style=""padding:30px 40px;"">
              <h2 style=""color:#1B4F72;font-size:20px;font-weight:700;margin:0 0 4px 0;"">رسالة جديدة من موقع بوصلة</h2>
              <p style=""color:#95A5A6;font-size:14px;margin:0 0 24px 0;"">تم استلام رسالة جديدة من صفحة اتصل بنا</p>

              <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA;border-radius:8px;padding:20px;"">
                <tr>
                  <td style=""padding:6px 0;"">
                    <span style=""color:#95A5A6;font-size:13px;display:block;"">الاسم</span>
                    <span style=""color:#2C3E50;font-size:16px;font-weight:600;"">{safeName}</span>
                  </td>
                </tr>
                <tr>
                  <td style=""padding:6px 0;"">
                    <span style=""color:#95A5A6;font-size:13px;display:block;"">البريد الإلكتروني</span>
                    <span style=""color:#2C3E50;font-size:16px;direction:ltr;display:inline-block;"">{safeEmail}</span>
                  </td>
                </tr>
                <tr>
                  <td style=""padding:6px 0;"">
                    <span style=""color:#95A5A6;font-size:13px;display:block;"">الموضوع</span>
                    <span style=""display:inline-block;background-color:#2E86AB15;color:#2E86AB;font-size:13px;font-weight:600;padding:4px 12px;border-radius:20px;"">{safeSubject}</span>
                  </td>
                </tr>
              </table>

              <div style=""margin-top:20px;background-color:#F7F9FA;border-radius:8px;padding:20px;border-right:3px solid #F39C12;"">
                <span style=""color:#95A5A6;font-size:13px;display:block;margin-bottom:8px;"">الرسالة</span>
                <p style=""color:#2C3E50;font-size:15px;line-height:1.7;margin:0;white-space:pre-wrap;"">{safeMessage}</p>
              </div>
            </td>
          </tr>

          <tr>
            <td style=""background-color:#1B4F72;padding:20px 40px;text-align:center;"">
              <p style=""color:rgba(255,255,255,0.7);font-size:13px;margin:0 0 4px 0;"">بوصلة — منصتك للاستشارات مع الخبراء المعتمدين</p>
              <p style=""color:rgba(255,255,255,0.5);font-size:12px;margin:0;"">تم إرسال هذه الرسالة تلقائياً من موقع بوصلة · الرجاء عدم الرد على هذا البريد</p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }

        private static string HtmlEncode(string text)
        {
            return System.Net.WebUtility.HtmlEncode(text ?? string.Empty);
        }
    }
}
