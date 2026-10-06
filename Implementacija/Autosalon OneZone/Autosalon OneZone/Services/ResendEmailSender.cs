using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace Autosalon_OneZone.Services
{
    public class ResendEmailOptions
    {
        public bool Enabled { get; set; }
        public string? ApiKey { get; set; }
        public string? FromEmail { get; set; }
    }

    public class ResendEmailSender : IEmailSender
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ResendEmailSender> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ResendEmailSender(
            HttpClient httpClient,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<ResendEmailSender> logger,
            IStringLocalizer<SharedResource>? localizer = null)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
            _localizer = localizer ?? new FallbackStringLocalizer<SharedResource>();
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string displayName, string resetLink, DateTime expiresAtUtc)
        {
            var enabled = _configuration.GetValue("Resend:Enabled", false);
            var apiKey = _configuration["Resend:ApiKey"];
            var fromEmail = _configuration["Resend:FromEmail"];

            if (!enabled)
            {
                if (_environment.IsDevelopment())
                {
                    _logger.LogWarning("Email delivery is disabled. Password reset email for {Email} was not sent.", toEmail);
                }
                else
                {
                    _logger.LogInformation("Email delivery is disabled; password reset email for {Email} was not sent.", toEmail);
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new InvalidOperationException("Email delivery is enabled, but Resend configuration is missing.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = JsonContent.Create(new
            {
                from = fromEmail,
                to = new[] { toEmail },
                subject = _localizer["PasswordResetEmailSubject"].Value,
                html = BuildPasswordResetHtml(displayName, resetLink, expiresAtUtc),
                text = BuildPasswordResetText(displayName, resetLink, expiresAtUtc)
            });

            using var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Password reset email queued for {Email}.", toEmail);
                return;
            }

            _logger.LogError(
                "Resend failed to queue password reset email for {Email}. Status: {StatusCode}.",
                toEmail,
                response.StatusCode);

            throw new InvalidOperationException("Password reset email could not be sent.");
        }

        public async Task SendSupportReplyEmailAsync(
            string toEmail,
            string displayName,
            string subject,
            string message,
            string detailsUrl,
            string culture)
        {
            var enabled = _configuration.GetValue("Resend:Enabled", false);
            if (!enabled)
            {
                return;
            }

            var apiKey = _configuration["Resend:ApiKey"];
            var fromEmail = _configuration["Resend:FromEmail"];
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromEmail))
            {
                throw new InvalidOperationException("Email delivery is enabled, but Resend configuration is missing.");
            }

            var emailCulture = CultureInfo.GetCultureInfo(
                culture.StartsWith("bs", StringComparison.OrdinalIgnoreCase) ? "bs-Latn-BA" : "en-US");
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = emailCulture;
                CultureInfo.CurrentUICulture = emailCulture;
                using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = JsonContent.Create(new
                {
                    from = fromEmail,
                    to = new[] { toEmail },
                    subject = string.Format(_localizer["SupportReplyEmailSubject"].Value, subject),
                    html = BuildSupportReplyHtml(displayName, subject, message, detailsUrl),
                    text = BuildSupportReplyText(displayName, subject, message, detailsUrl)
                });

                using var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Support reply email queued for {Email}.", toEmail);
                    return;
                }

                _logger.LogError(
                    "Resend failed to queue support reply email for {Email}. Status: {StatusCode}.",
                    toEmail,
                    response.StatusCode);
                throw new InvalidOperationException("Support reply email could not be sent.");
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        private string BuildPasswordResetHtml(string displayName, string resetLink, DateTime expiresAtUtc)
        {
            var safeName = HtmlEncoder.Default.Encode(string.IsNullOrWhiteSpace(displayName) ? _localizer["PasswordResetEmailGreetingNameFallback"].Value : displayName);
            var safeLink = HtmlEncoder.Default.Encode(resetLink);
            var expiry = HtmlEncoder.Default.Encode(expiresAtUtc.ToString("dd.MM.yyyy HH:mm 'UTC'"));
            var greeting = HtmlEncoder.Default.Encode(string.Format(_localizer["PasswordResetEmailGreeting"].Value, safeName));
            var validityText = HtmlEncoder.Default.Encode(string.Format(_localizer["PasswordResetEmailValidityText"].Value, expiry));

            return $$"""
                <!doctype html>
                <html>
                <head>
                  <meta charset="utf-8">
                  <meta name="viewport" content="width=device-width, initial-scale=1">
                  <title>{{HtmlEncoder.Default.Encode(_localizer["PasswordResetEmailTitle"].Value)}}</title>
                </head>
                <body style="margin:0;background:#f4f7fb;font-family:Inter,Arial,sans-serif;color:#0f172a;">
                  <div style="padding:28px 16px;">
                    <div style="max-width:640px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:26px;overflow:hidden;box-shadow:0 24px 70px rgba(15,23,42,0.10);">
                      <div style="background:linear-gradient(135deg,#0f172a,#2563eb);padding:34px 32px;color:#ffffff;">
                        <div style="font-size:12px;font-weight:900;letter-spacing:.14em;text-transform:uppercase;color:#bfdbfe;margin-bottom:14px;">Autosalon OneZone</div>
                        <h1 style="font-size:28px;line-height:1.05;margin:0 0 12px;font-weight:900;">{{HtmlEncoder.Default.Encode(_localizer["PasswordResetEmailTitle"].Value)}}</h1>
                        <p style="margin:0;color:#dbeafe;font-size:15px;line-height:1.6;">{{HtmlEncoder.Default.Encode(_localizer["PasswordResetEmailHeroText"].Value)}}</p>
                      </div>

                      <div style="padding:34px 32px;">
                        <div style="display:inline-block;padding:9px 14px;border-radius:999px;background:#eff6ff;color:#2563eb;font-size:12px;font-weight:900;letter-spacing:.08em;text-transform:uppercase;margin-bottom:22px;">Reset link</div>
                        <p style="font-size:16px;line-height:1.7;margin:0 0 18px;">{{greeting}}</p>
                        <p style="font-size:15px;line-height:1.8;margin:0 0 24px;color:#334155;">{{HtmlEncoder.Default.Encode(_localizer["PasswordResetEmailIntro"].Value)}}</p>

                        <div style="border:1px solid #dbe3ef;border-radius:18px;background:#f8fafc;padding:18px 20px;margin:0 0 26px;">
                          <div style="font-size:12px;font-weight:900;letter-spacing:.08em;text-transform:uppercase;color:#334155;margin-bottom:10px;">{{HtmlEncoder.Default.Encode(_localizer["PasswordResetEmailValidityTitle"].Value)}}</div>
                          <div style="font-size:14px;color:#334155;line-height:1.6;">{{validityText}}</div>
                        </div>

                        <div style="text-align:center;margin:28px 0;">
                          <a href="{{safeLink}}" style="display:inline-block;background:#2563eb;color:#ffffff;text-decoration:none;border-radius:16px;padding:14px 24px;font-weight:900;box-shadow:0 14px 30px rgba(37,99,235,.26);">{{HtmlEncoder.Default.Encode(_localizer["PasswordResetEmailButton"].Value)}}</a>
                        </div>

                        <p style="font-size:14px;line-height:1.7;color:#334155;margin:0 0 10px;">{{HtmlEncoder.Default.Encode(_localizer["PasswordResetEmailFallbackLink"].Value)}}</p>
                        <p style="word-break:break-all;margin:0 0 24px;"><a href="{{safeLink}}" style="color:#2563eb;">{{safeLink}}</a></p>
                        <p style="font-size:14px;line-height:1.7;color:#64748b;margin:0;">{{HtmlEncoder.Default.Encode(_localizer["PasswordResetEmailIgnore"].Value)}}</p>
                      </div>

                      <div style="padding:20px 32px;background:#f8fafc;border-top:1px solid #e5e7eb;color:#64748b;font-size:12px;">
                        {{HtmlEncoder.Default.Encode(_localizer["PasswordResetEmailFooter"].Value)}}
                      </div>
                    </div>
                  </div>
                </body>
                </html>
                """;
        }

        private string BuildPasswordResetText(string displayName, string resetLink, DateTime expiresAtUtc)
        {
            var name = string.IsNullOrWhiteSpace(displayName) ? _localizer["PasswordResetEmailGreetingNameFallback"].Value : displayName;
            return $"""
                {_localizer["PasswordResetEmailTextTitle"].Value}

                {string.Format(_localizer["PasswordResetEmailGreeting"].Value, name)}

                {_localizer["PasswordResetEmailIntro"].Value}
                {string.Format(_localizer["PasswordResetEmailValidityText"].Value, expiresAtUtc.ToString("dd.MM.yyyy HH:mm 'UTC'"))}

                {_localizer["PasswordResetEmailFallbackLink"].Value}
                {resetLink}

                {_localizer["PasswordResetEmailIgnore"].Value}
                """;
        }

        private string BuildSupportReplyHtml(string displayName, string subject, string message, string detailsUrl)
        {
            var name = string.IsNullOrWhiteSpace(displayName)
                ? _localizer["PasswordResetEmailGreetingNameFallback"].Value
                : displayName;
            var safeSubject = HtmlEncoder.Default.Encode(subject);
            var safeMessage = HtmlEncoder.Default.Encode(message)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\n", "<br>");
            var safeUrl = HtmlEncoder.Default.Encode(detailsUrl);

            return $$"""
                <!doctype html>
                <html>
                <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"></head>
                <body style="margin:0;background:#f4f7fb;font-family:Inter,Arial,sans-serif;color:#0f172a;">
                  <div style="padding:28px 16px;">
                    <div style="max-width:640px;margin:0 auto;background:#fff;border:1px solid #e5e7eb;border-radius:20px;overflow:hidden;">
                      <div style="background:#0f172a;padding:30px 32px;color:#fff;">
                        <div style="font-size:12px;font-weight:800;color:#bfdbfe;margin-bottom:12px;">Autosalon OneZone</div>
                        <h1 style="font-size:26px;margin:0;">{{HtmlEncoder.Default.Encode(_localizer["SupportReplyEmailTitle"].Value)}}</h1>
                      </div>
                      <div style="padding:32px;">
                        <p style="font-size:16px;line-height:1.7;">{{HtmlEncoder.Default.Encode(string.Format(_localizer["SupportReplyEmailGreeting"].Value, name))}}</p>
                        <p style="color:#475569;">{{HtmlEncoder.Default.Encode(_localizer["SupportReplyEmailIntro"].Value)}}</p>
                        <div style="font-weight:800;margin:24px 0 8px;">{{safeSubject}}</div>
                        <div style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:14px;padding:18px;line-height:1.7;">{{safeMessage}}</div>
                        <div style="text-align:center;margin:28px 0;">
                          <a href="{{safeUrl}}" style="display:inline-block;background:#2563eb;color:#fff;text-decoration:none;border-radius:12px;padding:13px 22px;font-weight:800;">{{HtmlEncoder.Default.Encode(_localizer["SupportReplyEmailButton"].Value)}}</a>
                        </div>
                        <p style="font-size:13px;color:#64748b;margin:0;">{{HtmlEncoder.Default.Encode(_localizer["SupportReplyEmailFooter"].Value)}}</p>
                      </div>
                    </div>
                  </div>
                </body>
                </html>
                """;
        }

        private string BuildSupportReplyText(string displayName, string subject, string message, string detailsUrl)
        {
            var name = string.IsNullOrWhiteSpace(displayName)
                ? _localizer["PasswordResetEmailGreetingNameFallback"].Value
                : displayName;
            return $"""
                {_localizer["SupportReplyEmailTitle"].Value}

                {string.Format(_localizer["SupportReplyEmailGreeting"].Value, name)}
                {_localizer["SupportReplyEmailIntro"].Value}

                {subject}
                {message}

                {_localizer["SupportReplyEmailButton"].Value}: {detailsUrl}
                {_localizer["SupportReplyEmailFooter"].Value}
                """;
        }
    }
}
