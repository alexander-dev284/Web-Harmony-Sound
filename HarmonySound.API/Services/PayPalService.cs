using PayPal.Api;
using Microsoft.Extensions.Configuration;

public interface IPayPalService
{
    Task<Payment> CreatePaymentAsync(decimal amount, string currency, string description);
    Task<Payment> ExecutePaymentAsync(string paymentId, string payerId);
}

public class PayPalService : IPayPalService
{
    private readonly IConfiguration _configuration;
    private readonly APIContext _apiContext;

    public PayPalService(IConfiguration configuration)
    {
        _configuration = configuration;

        var config = new Dictionary<string, string>
        {
            ["mode"] = _configuration["PayPal:Mode"],
            ["clientId"] = _configuration["PayPal:ClientId"],
            ["clientSecret"] = _configuration["PayPal:ClientSecret"]
        };

        var accessToken = new OAuthTokenCredential(config).GetAccessToken();
        _apiContext = new APIContext(accessToken) { Config = config };
    }

    public async Task<Payment> CreatePaymentAsync(decimal amount, string currency, string description)
    {
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
                        total = amount.ToString("F2"),
                        currency = currency
                    },
                    description = description
                }
            }
        };

        return payment.Create(_apiContext);
    }

    public async Task<Payment> ExecutePaymentAsync(string paymentId, string payerId)
    {
        var payment = Payment.Get(_apiContext, paymentId);
        var paymentExecution = new PaymentExecution { payer_id = payerId };
        return payment.Execute(_apiContext, paymentExecution);
    }
}