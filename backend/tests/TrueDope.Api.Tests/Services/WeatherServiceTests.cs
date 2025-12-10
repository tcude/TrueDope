using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using TrueDope.Api.Configuration;
using TrueDope.Api.Services;

namespace TrueDope.Api.Tests.Services;

public class WeatherServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<WeatherService>> _loggerMock;
    private readonly WeatherSettings _settings;

    public WeatherServiceTests()
    {
        _httpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandler.Object);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<WeatherService>>();
        _settings = new WeatherSettings
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://api.openweathermap.org/data/2.5/weather",
            CacheMinutes = 10
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _cache.Dispose();
    }

    private WeatherService CreateService()
    {
        var options = Options.Create(_settings);
        return new WeatherService(_httpClient, options, _cache, _loggerMock.Object);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, object? responseContent = null)
    {
        var json = responseContent != null
            ? JsonSerializer.Serialize(responseContent)
            : "";

        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(json)
            });
    }

    [Fact]
    public async Task GetWeatherAsync_WhenApiReturnsValidData_ShouldReturnWeatherDto()
    {
        // Arrange
        var owmResponse = new
        {
            main = new { temp = 293.15, humidity = 65, pressure = 1013 },
            wind = new { speed = 4.47, deg = 180 },
            weather = new[] { new { description = "clear sky" } }
        };
        SetupHttpResponse(HttpStatusCode.OK, owmResponse);
        var service = CreateService();

        // Act
        var result = await service.GetWeatherAsync(38.8977m, -77.0365m);

        // Assert
        result.Should().NotBeNull();
        result!.Temperature.Should().BeApproximately(68m, 1m); // 293.15K = ~68F
        result.Humidity.Should().Be(65);
        result.Pressure.Should().BeApproximately(29.92m, 0.1m);
        result.WindSpeed.Should().BeApproximately(10m, 0.5m); // 4.47 m/s = ~10 mph
        result.WindDirection.Should().Be(180);
        result.WindDirectionCardinal.Should().Be("S");
        result.Description.Should().Be("clear sky");
    }

    [Fact]
    public async Task GetWeatherAsync_WhenApiKeyNotConfigured_ShouldReturnNull()
    {
        // Arrange
        _settings.ApiKey = "";
        var service = CreateService();

        // Act
        var result = await service.GetWeatherAsync(38.8977m, -77.0365m);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWeatherAsync_WhenApiReturnsError_ShouldReturnNull()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.Unauthorized);
        var service = CreateService();

        // Act
        var result = await service.GetWeatherAsync(38.8977m, -77.0365m);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWeatherAsync_ShouldCacheResult()
    {
        // Arrange
        var owmResponse = new
        {
            main = new { temp = 293.15, humidity = 65, pressure = 1013 },
            wind = new { speed = 4.47, deg = 180 },
            weather = new[] { new { description = "clear sky" } }
        };
        SetupHttpResponse(HttpStatusCode.OK, owmResponse);
        var service = CreateService();

        // Act
        var result1 = await service.GetWeatherAsync(38.8977m, -77.0365m);
        var result2 = await service.GetWeatherAsync(38.8977m, -77.0365m);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();

        // Verify HTTP was only called once (second call should use cache)
        _httpMessageHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetWeatherAsync_WithElevation_ShouldCalculateDensityAltitude()
    {
        // Arrange
        var owmResponse = new
        {
            main = new { temp = 293.15, humidity = 65, pressure = 1013 },
            wind = new { speed = 4.47, deg = 180 },
            weather = new[] { new { description = "clear sky" } }
        };
        SetupHttpResponse(HttpStatusCode.OK, owmResponse);
        var service = CreateService();

        // Act
        var result = await service.GetWeatherAsync(38.8977m, -77.0365m, elevation: 500);

        // Assert
        result.Should().NotBeNull();
        result!.DensityAltitude.Should().NotBeNull();
        result.DensityAltitude.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetWeatherAsync_WithoutElevation_ShouldNotCalculateDensityAltitude()
    {
        // Arrange
        var owmResponse = new
        {
            main = new { temp = 293.15, humidity = 65, pressure = 1013 },
            wind = new { speed = 4.47, deg = 180 },
            weather = new[] { new { description = "clear sky" } }
        };
        SetupHttpResponse(HttpStatusCode.OK, owmResponse);
        var service = CreateService();

        // Act
        var result = await service.GetWeatherAsync(38.8977m, -77.0365m);

        // Assert
        result.Should().NotBeNull();
        result!.DensityAltitude.Should().BeNull();
    }

    [Theory]
    [InlineData(0, "N")]
    [InlineData(45, "NE")]
    [InlineData(90, "E")]
    [InlineData(135, "SE")]
    [InlineData(180, "S")]
    [InlineData(225, "SW")]
    [InlineData(270, "W")]
    [InlineData(315, "NW")]
    [InlineData(360, "N")]
    public async Task GetWeatherAsync_ShouldConvertWindDirectionToCardinal(int degrees, string expected)
    {
        // Arrange
        var owmResponse = new
        {
            main = new { temp = 293.15, humidity = 65, pressure = 1013 },
            wind = new { speed = 4.47, deg = degrees },
            weather = new[] { new { description = "clear sky" } }
        };
        SetupHttpResponse(HttpStatusCode.OK, owmResponse);

        // Clear cache between tests
        _cache.Remove($"weather:38.898:-77.037");

        var service = CreateService();

        // Act
        var result = await service.GetWeatherAsync(38.8977m, -77.0365m);

        // Assert
        result.Should().NotBeNull();
        result!.WindDirectionCardinal.Should().Be(expected);
    }

    [Fact]
    public async Task GetWeatherAsync_WhenInvalidJson_ShouldReturnNull()
    {
        // Arrange
        _httpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("not valid json")
            });
        var service = CreateService();

        // Act
        var result = await service.GetWeatherAsync(38.8977m, -77.0365m);

        // Assert
        result.Should().BeNull();
    }
}
