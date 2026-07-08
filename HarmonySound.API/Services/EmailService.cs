using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HarmonySound.API.Services
{
    // Envía correos usando la API HTTP de Brevo (https://api.brevo.com/v3/smtp/email).
    // Se usa HTTP (puerto 443) en lugar de SMTP porque Render bloquea los puertos SMTP
    // salientes (25/465/587) y una conexión directa a Gmail falla con "Network is unreachable".
    public class EmailService : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string BrevoEndpoint = "https://api.brevo.com/v3/smtp/email";

        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var apiKey = _configuration["Brevo:ApiKey"];
                // El remitente se reutiliza de la configuración SMTP ya existente.
                var senderEmail = _configuration["SmtpSettings:FromEmail"];
                var senderName = _configuration["SmtpSettings:FromName"] ?? "UniSound";

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(senderEmail))
                {
                    _logger.LogError("Configuración de correo incompleta (revisa Brevo:ApiKey y SmtpSettings:FromEmail)");
                    throw new InvalidOperationException("Email configuration is incomplete");
                }

                var payload = new
                {
                    sender = new { name = senderName, email = senderEmail },
                    to = new[] { new { email = email } },
                    subject = subject,
                    htmlContent = htmlMessage
                };

                var json = JsonSerializer.Serialize(payload);

                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(20);

                using var request = new HttpRequestMessage(HttpMethod.Post, BrevoEndpoint);
                request.Headers.TryAddWithoutValidation("api-key", apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Email sent successfully to {email}");
                }
                else
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to send email to {email}. Status: {(int)response.StatusCode}. Body: {body}");
                    throw new InvalidOperationException($"Brevo API returned {(int)response.StatusCode}: {body}");
                }
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
