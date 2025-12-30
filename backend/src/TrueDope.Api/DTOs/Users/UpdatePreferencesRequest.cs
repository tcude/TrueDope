using System.ComponentModel.DataAnnotations;

namespace TrueDope.Api.DTOs.Users;

public class UpdatePreferencesRequest
{
    [RegularExpression("^(yards|meters)$", ErrorMessage = "Distance unit must be 'yards' or 'meters'")]
    public string? DistanceUnit { get; set; }

    [RegularExpression("^(mil|moa)$", ErrorMessage = "Adjustment unit must be 'mil' or 'moa'")]
    public string? AdjustmentUnit { get; set; }

    [RegularExpression("^(fahrenheit|celsius)$", ErrorMessage = "Temperature unit must be 'fahrenheit' or 'celsius'")]
    public string? TemperatureUnit { get; set; }

    [RegularExpression("^(inhg|hpa)$", ErrorMessage = "Pressure unit must be 'inhg' or 'hpa'")]
    public string? PressureUnit { get; set; }

    [RegularExpression("^(fps|mps)$", ErrorMessage = "Velocity unit must be 'fps' or 'mps'")]
    public string? VelocityUnit { get; set; }

    [RegularExpression("^(system|light|dark)$", ErrorMessage = "Theme must be 'system', 'light', or 'dark'")]
    public string? Theme { get; set; }

    [RegularExpression("^(ctc|ete)$", ErrorMessage = "Group size method must be 'ctc' (center-to-center) or 'ete' (edge-to-edge)")]
    public string? GroupSizeMethod { get; set; }
}
