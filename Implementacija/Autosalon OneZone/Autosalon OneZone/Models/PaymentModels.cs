// Models/PaymentModels.cs
namespace Autosalon_OneZone.Models
{
    public class PaymentRequest
    {
        public string CardNumber { get; set; }
        public string ExpirationMonth { get; set; }
        public string ExpirationYear { get; set; }
        public string Cvv { get; set; }
        public decimal Amount { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string Description { get; set; }
        public int ProductId { get; set; }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; }
        public string Message { get; set; }
    }
}
