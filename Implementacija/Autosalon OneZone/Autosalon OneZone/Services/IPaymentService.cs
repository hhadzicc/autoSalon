// Services/IPaymentService.cs
using Autosalon_OneZone.Models;

namespace Autosalon_OneZone.Services
{
    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest paymentRequest);
    }
}
