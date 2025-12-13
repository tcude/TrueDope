using TrueDope.Api.DTOs.Geocoding;

namespace TrueDope.Api.Services;

public interface IGeocodingService
{
    /// <summary>
    /// Search for locations by query string (forward geocoding)
    /// </summary>
    Task<List<GeocodingSearchResultDto>> SearchAsync(string query, int limit = 5);

    /// <summary>
    /// Get address information from coordinates (reverse geocoding)
    /// </summary>
    Task<ReverseGeocodingResultDto?> ReverseGeocodeAsync(decimal latitude, decimal longitude);

    /// <summary>
    /// Get elevation for coordinates
    /// </summary>
    Task<ElevationResultDto?> GetElevationAsync(decimal latitude, decimal longitude);
}
