namespace TrueDope.Api.Configuration;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "TrueDope";
    public string Audience { get; set; } = "TrueDope";
    public int AccessTokenExpirationMinutes { get; set; } = 60;  // TEMPORARY: Set to 2 min for testing refresh flow
    public int RefreshTokenExpirationDays { get; set; } = 30;
}
