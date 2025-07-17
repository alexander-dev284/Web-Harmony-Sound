using PayPal.Api;
using Microsoft.Extensions.Configuration;
using System.Globalization;

public interface IPayPalService
{
    Task<Payment> CreatePaymentAsync(decimal amount, string currency, string description);
    Task<Payment> ExecutePaymentAsync(string paymentId, string payerId);
}

public class PayPalService : IPayPalService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayPalService> _logger;
    private readonly APIContext _apiContext;

    public PayPalService(IConfiguration configuration, ILogger<PayPalService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        try
        {
            var config = new Dictionary<string, string>
            {
                ["mode"] = _configuration["PayPal:Mode"],
                ["clientId"] = _configuration["PayPal:ClientId"],
                ["clientSecret"] = _configuration["PayPal:ClientSecret"]
            };

            _logger.LogInformation($"PayPal configurado en modo: {config["mode"]}");
            _logger.LogInformation($"PayPal Client ID: {config["clientId"]?.Substring(0, Math.Min(10, config["clientId"]?.Length ?? 0))}...");

            var accessToken = new OAuthTokenCredential(config).GetAccessToken();
            _apiContext = new APIContext(accessToken) { Config = config };
            
            _logger.LogInformation("PayPal APIContext creado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al configurar PayPal");
            throw;
        }
    }

    public async Task<Payment> CreatePaymentAsync(decimal amount, string currency, string description)
    {
        try
        {
            _logger.LogInformation($"Creando pago PayPal: {amount} {currency} - {description}");
            
            var payment = new Payment
            {
                intent = "sale",
                payer = new Payer { payment_method = "paypal" },
                redirect_urls = new RedirectUrls
                {
                    return_url = _configuration["PayPal:ReturnUrl"],
                    cancel_url = _configuration["PayPal:CancelUrl"]
                },
                transactions = new List<Transaction>
                {
                    new Transaction
                    {
                        amount = new Amount
                        {
                            total = amount.ToString("F2", CultureInfo.InvariantCulture),
                            currency = currency
                        },
                        description = description
                    }
                }
            };

            _logger.LogInformation($"Return URL: {_configuration["PayPal:ReturnUrl"]}");
            _logger.LogInformation($"Cancel URL: {_configuration["PayPal:CancelUrl"]}");

            var createdPayment = payment.Create(_apiContext);
            _logger.LogInformation($"Pago creado exitosamente: {createdPayment.id}");
            
            return createdPayment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear pago PayPal");
            throw;
        }
    }

    public async Task<Payment> ExecutePaymentAsync(string paymentId, string payerId)
    {
        try
        {
            _logger.LogInformation($"Ejecutando pago PayPal: {paymentId} con PayerID: {payerId}");
            
            var payment = Payment.Get(_apiContext, paymentId);
            var paymentExecution = new PaymentExecution { payer_id = payerId };
            
            var executedPayment = payment.Execute(_apiContext, paymentExecution);
            _logger.LogInformation($"Pago ejecutado exitosamente: {executedPayment.id}");
            
            return executedPayment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al ejecutar pago PayPal");
            throw;
        }
    }
}