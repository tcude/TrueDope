using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using TrueDope.Api.DTOs.Geocoding;

namespace TrueDope.Api.Services;

public class GeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GeocodingService> _logger;

    private const string NominatimBaseUrl = "https://nominatim.openstreetmap.org";
    private const string ElevationApiUrl = "https://api.open-elevation.com/api/v1/lookup";
    private const string UserAgent = "TrueDope/2.0 (ballistics-logging-app)";

    // Cache durations
    private static readonly TimeSpan SearchCacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ElevationCacheDuration = TimeSpan.FromDays(30);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeocodingService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<GeocodingService> logger)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<GeocodingSearchResultDto>> SearchAsync(string query, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<GeocodingSearchResultDto>();
        }

        var cacheKey = $"geocode:search:{query.ToLowerInvariant()}:{limit}";

        if (_cache.TryGetValue(cacheKey, out List<GeocodingSearchResultDto>? cached) && cached != null)
        {
            _logger.LogDebug("Returning cached geocoding results for '{Query}'", query);
            return cached;
        }

        try
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"{NominatimBaseUrl}/search?q={encodedQuery}&format=json&limit={limit}&countrycodes=us";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Nominatim search returned {StatusCode} for query '{Query}'",
                    response.StatusCode, query);
                return new List<GeocodingSearchResultDto>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<NominatimSearchResult>>(json, JsonOptions);

            if (results == null || results.Count == 0)
            {
                _logger.LogDebug("No geocoding results found for '{Query}'", query);
                return new List<GeocodingSearchResultDto>();
            }

            var dtos = results
                .Where(r => !string.IsNullOrEmpty(r.Lat) && !string.IsNullOrEmpty(r.Lon))
                .Select(r => new GeocodingSearchResultDto
                {
                    DisplayName = r.Display_name ?? string.Empty,
                    Latitude = decimal.Parse(r.Lat!),
                    Longitude = decimal.Parse(r.Lon!),
                    Type = r.Type,
                    Category = r.Category
                })
                .ToList();

            _cache.Set(cacheKey, dtos, SearchCacheDuration);
            _logger.LogInformation("Geocoding search for '{Query}' returned {Count} results", query, dtos.Count);

            return dtos;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to search Nominatim for '{Query}'", query);
            return new List<GeocodingSearchResultDto>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Nominatim response for '{Query}'", query);
            return new List<GeocodingSearchResultDto>();
        }
    }

    public async Task<ReverseGeocodingResultDto?> ReverseGeocodeAsync(decimal latitude, decimal longitude)
    {
        // Round coordinates for cache key
        var cacheKey = $"geocode:reverse:{Math.Round(latitude, 4)}:{Math.Round(longitude, 4)}";

        if (_cache.TryGetValue(cacheKey, out ReverseGeocodingResultDto? cached) && cached != null)
        {
            _logger.LogDebug("Returning cached reverse geocoding for {Lat}, {Lon}", latitude, longitude);
            return cached;
        }

        try
        {
            var url = $"{NominatimBaseUrl}/reverse?lat={latitude}&lon={longitude}&format=json";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Nominatim reverse lookup returned {StatusCode} for {Lat}, {Lon}",
                    response.StatusCode, latitude, longitude);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<NominatimReverseResult>(json, JsonOptions);

            if (result == null || string.IsNullOrEmpty(result.Display_name))
            {
                _logger.LogDebug("No reverse geocoding result for {Lat}, {Lon}", latitude, longitude);
                return null;
            }

            var dto = new ReverseGeocodingResultDto
            {
                DisplayName = result.Display_name,
                City = result.Address?.City ?? result.Address?.Town ?? result.Address?.Village,
                State = result.Address?.State,
                Country = result.Address?.Country
            };

            _cache.Set(cacheKey, dto, SearchCacheDuration);
            _logger.LogInformation("Reverse geocoding for {Lat}, {Lon}: {DisplayName}",
                latitude, longitude, dto.DisplayName);

            return dto;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed reverse geocoding for {Lat}, {Lon}", latitude, longitude);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse reverse geocoding response for {Lat}, {Lon}",
                latitude, longitude);
            return null;
        }
    }

    public async Task<ElevationResultDto?> GetElevationAsync(decimal latitude, decimal longitude)
    {
        // Round to 3 decimal places (~110m precision) for cache key
        var roundedLat = Math.Round(latitude, 3);
        var roundedLon = Math.Round(longitude, 3);
        var cacheKey = $"elevation:{roundedLat}:{roundedLon}";

        if (_cache.TryGetValue(cacheKey, out ElevationResultDto? cached) && cached != null)
        {
            _logger.LogDebug("Returning cached elevation for {Lat}, {Lon}", latitude, longitude);
            return new ElevationResultDto
            {
                Elevation = cached.Elevation,
                Source = "cache"
            };
        }

        try
        {
            var url = $"{ElevationApiUrl}?locations={latitude},{longitude}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Open-Elevation API returned {StatusCode} for {Lat}, {Lon}",
                    response.StatusCode, latitude, longitude);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OpenElevationResponse>(json, JsonOptions);

            if (result?.Results == null || result.Results.Count == 0)
            {
                _logger.LogDebug("No elevation result for {Lat}, {Lon}", latitude, longitude);
                return null;
            }

            // Convert meters to feet
            var elevationMeters = result.Results[0].Elevation;
            var elevationFeet = (decimal)(elevationMeters * 3.28084);

            var dto = new ElevationResultDto
            {
                Elevation = Math.Round(elevationFeet, 0),
                Source = "open-elevation"
            };

            _cache.Set(cacheKey, dto, ElevationCacheDuration);
            _logger.LogInformation("Elevation for {Lat}, {Lon}: {Elevation} ft",
                latitude, longitude, dto.Elevation);

            return dto;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get elevation for {Lat}, {Lon}", latitude, longitude);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse elevation response for {Lat}, {Lon}",
                latitude, longitude);
            return null;
        }
    }
}
