using Autosalon_OneZone.Models;

namespace Autosalon_OneZone.Services
{
    public class MockPaymentService : IPaymentService
    {
        private readonly ILogger<MockPaymentService> _logger;

        public MockPaymentService(ILogger<MockPaymentService> logger)
        {
            _logger = logger;
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
                    Message = "Iznos placanja mora biti veci od nule."
                });
            }

            if (cleanCardNumber.Length < 12 || cleanCardNumber.Length > 19)
            {
                return Task.FromResult(new PaymentResult
                {
                    Success = false,
                    TransactionId = "mock_invalid_card",
                    Message = "Neispravan broj kartice."
                });
            }

            if (cleanCardNumber == "4000000000000002")
            {
                return Task.FromResult(new PaymentResult
                {
                    Success = false,
                    TransactionId = "mock_card_declined",
                    Message = "Transakcija odbijena u demo modu."
                });
            }

            var transactionId = $"mock_{Guid.NewGuid():N}";
            _logger.LogInformation("Mock payment accepted for {Amount}. Transaction: {TransactionId}", paymentRequest.Amount, transactionId);

            return Task.FromResult(new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Message = "Placanje uspjesno obradjeno u demo modu."
            });
        }
    }
}
