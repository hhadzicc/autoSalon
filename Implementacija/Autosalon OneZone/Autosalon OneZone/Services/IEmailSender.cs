namespace Autosalon_OneZone.Services
{
    public interface IEmailSender
    {
        Task SendPasswordResetEmailAsync(string toEmail, string displayName, string resetLink, DateTime expiresAtUtc);
    }
}
