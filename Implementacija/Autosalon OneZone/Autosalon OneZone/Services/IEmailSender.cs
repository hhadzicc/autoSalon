namespace Autosalon_OneZone.Services
{
    public interface IEmailSender
    {
        Task SendPasswordResetEmailAsync(string toEmail, string displayName, string resetLink, DateTime expiresAtUtc);
        Task SendSupportReplyEmailAsync(
            string toEmail,
            string displayName,
            string subject,
            string message,
            string detailsUrl,
            string culture);
    }
}
