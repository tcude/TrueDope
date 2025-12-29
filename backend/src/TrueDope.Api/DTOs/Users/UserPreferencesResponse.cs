namespace TrueDope.Api.DTOs.Users;

public class UserPreferencesResponse
{
    public string DistanceUnit { get; set; } = "yards";
    public string AdjustmentUnit { get; set; } = "mil";
    public string TemperatureUnit { get; set; } = "fahrenheit";
    public string PressureUnit { get; set; } = "inhg";
    public string VelocityUnit { get; set; } = "fps";
    public string Theme { get; set; } = "system";
    public string GroupSizeMethod { get; set; } = "ctc";
}
