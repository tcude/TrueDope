namespace TrueDope.Api.Configuration;

public class WeatherSettings
{
    public const string SectionName = "OpenWeatherMap";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openweathermap.org/data/2.5/weather";
    public int CacheMinutes { get; set; } = 10;
}
