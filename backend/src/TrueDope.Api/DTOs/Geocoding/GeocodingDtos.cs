namespace TrueDope.Api.DTOs.Geocoding;

/// <summary>
/// Result from forward geocoding search
/// </summary>
public class GeocodingSearchResultDto
{
    public string DisplayName { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Type { get; set; }
    public string? Category { get; set; }
}

/// <summary>
/// Result from reverse geocoding lookup
/// </summary>
public class ReverseGeocodingResultDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
}

/// <summary>
/// Elevation lookup result
/// </summary>
public class ElevationResultDto
{
    public decimal Elevation { get; set; }  // In feet
    public string Source { get; set; } = "open-elevation";
}

// Internal DTOs for Nominatim API response parsing
internal class NominatimSearchResult
{
    public string? Display_name { get; set; }
    public string? Lat { get; set; }
    public string? Lon { get; set; }
    public string? Type { get; set; }
    public string? Category { get; set; }
}

internal class NominatimReverseResult
{
    public string? Display_name { get; set; }
    public NominatimAddress? Address { get; set; }
}

internal class NominatimAddress
{
    public string? City { get; set; }
    public string? Town { get; set; }
    public string? Village { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
}

// Internal DTO for Open-Elevation API response parsing
internal class OpenElevationResponse
{
    public List<OpenElevationResult>? Results { get; set; }
}

internal class OpenElevationResult
{
    public double Elevation { get; set; }  // In meters
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
