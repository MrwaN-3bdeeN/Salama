using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Salama.Helpers
{
    public class EmailService
    {
        private readonly SmtpSettings _settings;

        public EmailService(IOptions<SmtpSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendVerificationCodeAsync(string toEmail, string code)
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword)
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = "Salama - Email Verification Code",
                Body = $@"
                    <div style='font-family:Arial,sans-serif;max-width:400px;margin:auto;padding:20px'>
                        <h2 style='color:#0d6efd'>Salama</h2>
                        <p>Your email verification code is:</p>
                        <div style='font-size:24px;font-weight:bold;color:#0d6efd;letter-spacing:8px;text-align:center;padding:15px;background:#f8f9fa;border-radius:8px'>{code}</div>
                        <p style='color:#6c757d;font-size:12px'>This code expires in 10 minutes. If you didn't request this, ignore this email.</p>
                    </div>",
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);

            await client.SendMailAsync(mail);
        }
    }
}
