using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace HarmonySound.API.Services
{
    public class EmailService : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");

                // Validar configuración
                if (string.IsNullOrEmpty(smtpSettings["Server"]) || 
                    string.IsNullOrEmpty(smtpSettings["Username"]) || 
                    string.IsNullOrEmpty(smtpSettings["Password"]))
                {
                    _logger.LogError("SMTP configuration is incomplete");
                    throw new InvalidOperationException("SMTP configuration is incomplete");
                }

                using var client = new SmtpClient(smtpSettings["Server"], int.Parse(smtpSettings["Port"]))
                {
                    EnableSsl = bool.Parse(smtpSettings["EnableSsl"]),
                    Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"])
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpSettings["FromEmail"], smtpSettings["FromName"]),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true // Para soportar HTML en invitaciones
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent successfully to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {email}");
                
                // En desarrollo, no fallar por errores de email
                if (_configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    _logger.LogWarning("Email sending failed in development mode - continuing...");
                    return;
                }
                
                throw;
            }
        }
    }
}