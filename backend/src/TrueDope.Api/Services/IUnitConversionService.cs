using TrueDope.Api.Data.Entities;

namespace TrueDope.Api.Services;

public interface IUnitConversionService
{
    // Distance: canonical = yards
    double ConvertDistance(double yards, DistanceUnit targetUnit);
    double ConvertDistanceToCanonical(double value, DistanceUnit sourceUnit);
    string FormatDistance(double yards, DistanceUnit unit, int decimals = 0);

    // Temperature: canonical = Fahrenheit
    double ConvertTemperature(double fahrenheit, TemperatureUnit targetUnit);
    double ConvertTemperatureToCanonical(double value, TemperatureUnit sourceUnit);
    string FormatTemperature(double fahrenheit, TemperatureUnit unit, int decimals = 0);

    // Pressure: canonical = inHg
    double ConvertPressure(double inHg, PressureUnit targetUnit);
    double ConvertPressureToCanonical(double value, PressureUnit sourceUnit);
    string FormatPressure(double inHg, PressureUnit unit, int decimals = 2);

    // Velocity: canonical = fps
    double ConvertVelocity(double fps, VelocityUnit targetUnit);
    double ConvertVelocityToCanonical(double value, VelocityUnit sourceUnit);
    string FormatVelocity(double fps, VelocityUnit unit, int decimals = 0);

    // Adjustment: canonical = MIL
    double ConvertAdjustment(double mils, AdjustmentUnit targetUnit);
    double ConvertAdjustmentToCanonical(double value, AdjustmentUnit sourceUnit);
    string FormatAdjustment(double mils, AdjustmentUnit unit, int decimals = 1);
}
