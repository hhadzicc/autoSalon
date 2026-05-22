using Autosalon_OneZone.Models;
using Microsoft.Extensions.Localization;

namespace Autosalon_OneZone.Services
{
    public class MockPaymentService : IPaymentService
    {
        private readonly ILogger<MockPaymentService> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public MockPaymentService(ILogger<MockPaymentService> logger, IStringLocalizer<SharedResource>? localizer = null)
        {
            _logger = logger;
            _localizer = localizer ?? new FallbackStringLocalizer<SharedResource>();
        }

        public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest paymentRequest)
        {
            var cardNumber = paymentRequest.CardNumber ?? "";
            var cleanCardNumber = new string(cardNumber.Where(char.IsDigit).ToArray());

            if (paymentRequest.Amount <= 0)
            {
                return Task.FromResult(new PaymentResult
                {
                    Success = false,
                    TransactionId = "mock_invalid_amount",
                    Message = _localizer["PaymentAmountPositive"].Value
                });
            }

            if (cleanCardNumber.Length < 12 || cleanCardNumber.Length > 19)
            {
                return Task.FromResult(new PaymentResult
                {
                    Success = false,
                    TransactionId = "mock_invalid_card",
                    Message = _localizer["PaymentInvalidCard"].Value
                });
            }

            if (cleanCardNumber == "4000000000000002")
            {
                return Task.FromResult(new PaymentResult
                {
                    Success = false,
                    TransactionId = "mock_card_declined",
                    Message = _localizer["PaymentDeclinedDemo"].Value
                });
            }

            var transactionId = $"mock_{Guid.NewGuid():N}";
            _logger.LogInformation("Mock payment accepted for {Amount}. Transaction: {TransactionId}", paymentRequest.Amount, transactionId);

            return Task.FromResult(new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Message = _localizer["PaymentSuccessDemo"].Value
            });
        }
    }
}
