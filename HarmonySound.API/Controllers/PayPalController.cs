using Microsoft.AspNetCore.Mvc;
using HarmonySound.API.Services;
using PayPal.Api;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayPalController : ControllerBase
    {
        private readonly IPayPalService _payPalService;
        private readonly ILogger<PayPalController> _logger;

        public PayPalController(IPayPalService payPalService, ILogger<PayPalController> logger)
        {
            _payPalService = payPalService;
            _logger = logger;
        }

        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                _logger.LogInformation($"Creating payment for amount: {request.Amount}");

                var payment = await _payPalService.CreatePaymentAsync(
                    request.Amount,
                    request.Currency,
                    request.Description
                );

                var approvalUrl = payment.links.FirstOrDefault(l => l.rel == "approval_url")?.href;

                _logger.LogInformation($"Payment created with ID: {payment.id}");

                return Ok(new
                {
                    paymentId = payment.id,
                    approvalUrl = approvalUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal payment");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("execute-payment")]
        public async Task<IActionResult> ExecutePayment([FromBody] ExecutePaymentRequest request)
        {
            try
            {
                _logger.LogInformation($"Executing payment: {request.PaymentId}");

                var payment = await _payPalService.ExecutePaymentAsync(
                    request.PaymentId,
                    request.PayerId
                );

                _logger.LogInformation($"Payment executed with state: {payment.state}");

                return Ok(new
                {
                    paymentId = payment.id,
                    state = payment.state,
                    amount = payment.transactions.FirstOrDefault()?.amount?.total,
                    currency = payment.transactions.FirstOrDefault()?.amount?.currency
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing PayPal payment");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class CreatePaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Description { get; set; }
    }

    public class ExecutePaymentRequest
    {
        public string PaymentId { get; set; }
        public string PayerId { get; set; }
    }
}