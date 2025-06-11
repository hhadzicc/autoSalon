// Services/StripePaymentService.cs
using Stripe;
using Autosalon_OneZone.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Autosalon_OneZone.Services
{
    public class StripePaymentService : IPaymentService
    {
        private readonly StripeSettings _stripeSettings;
        private readonly ILogger<StripePaymentService> _logger;

        public StripePaymentService(IOptions<StripeSettings> stripeSettings, ILogger<StripePaymentService> logger)
        {
            _stripeSettings = stripeSettings.Value;
            _logger = logger;
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest paymentRequest)
        {
            try
            {
                // Ukloni sve razmake i nečifre iz broja kartice
                string cleanCardNumber = new string(paymentRequest.CardNumber.Where(char.IsDigit).ToArray());
                string stripeToken;

                // Odaberi odgovarajući token na osnovu broja kartice
                // Za više tokena vidjeti: https://stripe.com/docs/testing
                switch (cleanCardNumber)
                {
                    case "4242424242424242":
                        stripeToken = "tok_visa"; // Uspješna Visa
                        break;
                    case "5555555555554444":
                        stripeToken = "tok_mastercard"; // Uspješna Mastercard
                        break;
                    case "378282246310005":
                        stripeToken = "tok_amex"; // Uspješna American Express
                        break;
                    case "6011111111111117":
                        stripeToken = "tok_discover"; // Uspješna Discover
                        break;
                    case "3056930009020004":
                        stripeToken = "tok_diners"; // Uspješna Diners Club
                        break;
                    case "4000000000009995":
                        stripeToken = "tok_chargeDeclined"; // Odbijena transakcija
                        break;
                    case "4000000000000002":
                        stripeToken = "tok_chargeDeclinedInsufficientFunds"; // Nedovoljno sredstava
                        break;
                    case "4000000000000028":
                        stripeToken = "tok_chargeDeclinedLostCard"; // Izgubljena kartica
                        break;
                    case "4000000000000036":
                        stripeToken = "tok_chargeDeclinedStolenCard"; // Ukradena kartica
                        break;
                    case "4000000000000101":
                        stripeToken = "tok_chargeDeclinedExpiredCard"; // Istekla kartica
                        break;
                    case "4000000000000341":
                        stripeToken = "tok_chargeDeclinedFraudulent"; // Sumnja na prevaru
                        break;
                    default:
                        if (cleanCardNumber.Length == 16)
                        {
                            // Za sve druge kartice s ispravnom dužinom, koristimo test token
                            // koji će biti odbijen zbog nevalidnog broja kartice
                            stripeToken = "tok_chargeDeclinedProcessingError";
                        }
                        else
                        {
                            // Za kartice s pogrešnom dužinom, vraćamo mock odgovor bez pozivanja Stripe API-a
                            _logger.LogInformation("Odbijanje kartice s neispravnom dužinom: {Length} cifara", cleanCardNumber.Length);
                            return new PaymentResult
                            {
                                Success = false,
                                TransactionId = "mock_invalid_length",
                                Message = "Neispravan broj kartice - mora imati 16 cifara."
                            };
                        }
                        break;
                }

                _logger.LogInformation("Procesiranje plaćanja sa tokenom {Token} za iznos: {Amount}", stripeToken, paymentRequest.Amount);

                // Maskiranje broja kartice za metapodatke
                string maskedCardNumber = "xxxx-xxxx-xxxx-" +
                    (cleanCardNumber.Length >= 4 ? cleanCardNumber.Substring(cleanCardNumber.Length - 4) : "????");

                // Koristimo token za kreiranje naplate
                var options = new ChargeCreateOptions
                {
                    Amount = (long)(paymentRequest.Amount * 100), // Pretvaranje u cente
                    Currency = "eur",
                    Source = stripeToken,
                    Description = $"Plaćanje za {paymentRequest.Description}",
                    ReceiptEmail = paymentRequest.Email,
                    Metadata = new Dictionary<string, string>
                    {
                        { "CustomerName", paymentRequest.CustomerName },
                        { "ProductId", paymentRequest.ProductId.ToString() },
                        { "CardNumber", maskedCardNumber }
                    }
                };

                var service = new ChargeService();

                try
                {
                    // Stvarno poslati zahtjev Stripe API-ju
                    Charge charge = await service.CreateAsync(options);

                    _logger.LogInformation("Uspješno procesirana transakcija ID: {ChargeId}", charge.Id);
                    return new PaymentResult
                    {
                        Success = true,
                        TransactionId = charge.Id,
                        Message = charge.Status
                    };
                }
                catch (StripeException ex) when (ex.StripeError?.DeclineCode != null ||
                                              ex.StripeError?.Code == "card_declined" ||
                                              ex.StripeError?.Type == "card_error")
                {
                    // Ovo hvata odbijene kartice i druge greške vezane za kartice
                    _logger.LogWarning("Odbijena kartica: {Message}, Kod: {DeclineCode}",
                        ex.Message, ex.StripeError?.DeclineCode);

                    // Formatiraj poruku za korisnika
                    string userMessage;
                    switch (ex.StripeError?.DeclineCode)
                    {
                        case "insufficient_funds":
                            userMessage = "Transakcija odbijena - nedovoljno sredstava na kartici.";
                            break;
                        case "lost_card":
                            userMessage = "Transakcija odbijena - kartica je prijavljena kao izgubljena.";
                            break;
                        case "stolen_card":
                            userMessage = "Transakcija odbijena - kartica je prijavljena kao ukradena.";
                            break;
                        case "expired_card":
                            userMessage = "Transakcija odbijena - kartica je istekla.";
                            break;
                        case "fraudulent":
                            userMessage = "Transakcija odbijena - sumnja na prevaru.";
                            break;
                        default:
                            userMessage = $"Transakcija odbijena. Kartica nije validna";
                            break;
                    }

                    return new PaymentResult
                    {
                        Success = false,
                        TransactionId = "declined_transaction",
                        Message = userMessage
                    };
                }
            }
            catch (StripeException ex)
            {
                // Druge Stripe greške (ne vezane za odbijanje kartice)
                _logger.LogError(ex, "Greška u Stripe API-ju: {Message}", ex.Message);
                return new PaymentResult
                {
                    Success = false,
                    Message = $"Greška u plaćanju: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                // Opće greške
                _logger.LogError(ex, "Opća greška: {Message}", ex.Message);
                return new PaymentResult
                {
                    Success = false,
                    Message = "Došlo je do greške prilikom obrade plaćanja. Molimo pokušajte ponovo."
                };
            }
        }
    }
}
