namespace TrueDope.Api.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetToken, string resetUrl);
    Task SendEmailAsync(string to, string subject, string htmlBody);
}
