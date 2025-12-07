using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TrueDope.Api.Configuration;

namespace TrueDope.Api.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly IWebHostEnvironment _environment;

    public EmailService(
        IOptions<SmtpSettings> smtpSettings,
        ILogger<EmailService> logger,
        IWebHostEnvironment environment)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
        _environment = environment;
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken, string resetUrl)
    {
        var fullResetUrl = $"{resetUrl}?token={Uri.EscapeDataString(resetToken)}";

        var subject = "Reset Your TrueDope Password";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #2563eb; color: white; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Password Reset Request</h2>
        <p>We received a request to reset your TrueDope account password.</p>
        <p>Click the button below to reset your password:</p>
        <a href=""{fullResetUrl}"" class=""button"">Reset Password</a>
        <p>Or copy and paste this link into your browser:</p>
        <p style=""word-break: break-all; color: #666;"">{fullResetUrl}</p>
        <p><strong>This link will expire in 1 hour.</strong></p>
        <p>If you didn't request a password reset, you can safely ignore this email.</p>
        <div class=""footer"">
            <p>This email was sent from TrueDope. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, subject, htmlBody);
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        // In development without SMTP configured, log to console instead of sending
        if (_environment.IsDevelopment() && string.IsNullOrEmpty(_smtpSettings.Username))
        {
            _logger.LogInformation(
                "Email would be sent to {To}: {Subject}\n{Body}",
                to, subject, htmlBody);
            return;
        }

        try
        {
            using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
            {
                Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                EnableSsl = _smtpSettings.UseSsl
            };

            var message = new MailMessage
            {
                From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(to);

            await client.SendMailAsync(message);

            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            throw;
        }
    }
}
