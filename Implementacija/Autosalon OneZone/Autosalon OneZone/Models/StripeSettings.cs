namespace Autosalon_OneZone.Models
{
    public class StripeSettings
    {
        public string SecretKey { get; set; } = "";
        public string PublishableKey { get; set; } = "";
        public bool UseMockPayments { get; set; }
    }
}
