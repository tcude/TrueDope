namespace TrueDope.Api.Configuration;

public class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "mail.smtp2go.com";
    public int Port { get; set; } = 2525;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@truedope.io";
    public string FromName { get; set; } = "TrueDope";
    public bool UseSsl { get; set; } = true;
}
