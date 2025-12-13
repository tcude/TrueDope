using TrueDope.Api.Data.Entities;

namespace TrueDope.Api.Services;

public class UnitConversionService : IUnitConversionService
{
    // Conversion constants
    private const double YardsToMeters = 0.9144;
    private const double MetersToYards = 1.09361;
    private const double InHgToHPa = 33.8639;
    private const double HPaToInHg = 0.02953;
    private const double FpsToMps = 0.3048;
    private const double MpsToFps = 3.28084;
    private const double MilToMoa = 3.438;
    private const double MoaToMil = 0.2909;

    #region Distance

    public double ConvertDistance(double yards, DistanceUnit targetUnit)
    {
        return targetUnit switch
        {
            DistanceUnit.Yards => yards,
            DistanceUnit.Meters => yards * YardsToMeters,
            _ => yards
        };
    }

    public double ConvertDistanceToCanonical(double value, DistanceUnit sourceUnit)
    {
        return sourceUnit switch
        {
            DistanceUnit.Yards => value,
            DistanceUnit.Meters => value * MetersToYards,
            _ => value
        };
    }

    public string FormatDistance(double yards, DistanceUnit unit, int decimals = 0)
    {
        var converted = ConvertDistance(yards, unit);
        var suffix = unit switch
        {
            DistanceUnit.Yards => "yd",
            DistanceUnit.Meters => "m",
            _ => "yd"
        };
        return $"{converted.ToString($"F{decimals}")} {suffix}";
    }

    #endregion

    #region Temperature

    public double ConvertTemperature(double fahrenheit, TemperatureUnit targetUnit)
    {
        return targetUnit switch
        {
            TemperatureUnit.Fahrenheit => fahrenheit,
            TemperatureUnit.Celsius => (fahrenheit - 32) * 5.0 / 9.0,
            _ => fahrenheit
        };
    }

    public double ConvertTemperatureToCanonical(double value, TemperatureUnit sourceUnit)
    {
        return sourceUnit switch
        {
            TemperatureUnit.Fahrenheit => value,
            TemperatureUnit.Celsius => value * 9.0 / 5.0 + 32,
            _ => value
        };
    }

    public string FormatTemperature(double fahrenheit, TemperatureUnit unit, int decimals = 0)
    {
        var converted = ConvertTemperature(fahrenheit, unit);
        var suffix = unit switch
        {
            TemperatureUnit.Fahrenheit => "°F",
            TemperatureUnit.Celsius => "°C",
            _ => "°F"
        };
        return $"{converted.ToString($"F{decimals}")}{suffix}";
    }

    #endregion

    #region Pressure

    public double ConvertPressure(double inHg, PressureUnit targetUnit)
    {
        return targetUnit switch
        {
            PressureUnit.InHg => inHg,
            PressureUnit.HPa => inHg * InHgToHPa,
            _ => inHg
        };
    }

    public double ConvertPressureToCanonical(double value, PressureUnit sourceUnit)
    {
        return sourceUnit switch
        {
            PressureUnit.InHg => value,
            PressureUnit.HPa => value * HPaToInHg,
            _ => value
        };
    }

    public string FormatPressure(double inHg, PressureUnit unit, int decimals = 2)
    {
        var converted = ConvertPressure(inHg, unit);
        var suffix = unit switch
        {
            PressureUnit.InHg => " inHg",
            PressureUnit.HPa => " hPa",
            _ => " inHg"
        };
        return $"{converted.ToString($"F{decimals}")}{suffix}";
    }

    #endregion

    #region Velocity

    public double ConvertVelocity(double fps, VelocityUnit targetUnit)
    {
        return targetUnit switch
        {
            VelocityUnit.FPS => fps,
            VelocityUnit.MPS => fps * FpsToMps,
            _ => fps
        };
    }

    public double ConvertVelocityToCanonical(double value, VelocityUnit sourceUnit)
    {
        return sourceUnit switch
        {
            VelocityUnit.FPS => value,
            VelocityUnit.MPS => value * MpsToFps,
            _ => value
        };
    }

    public string FormatVelocity(double fps, VelocityUnit unit, int decimals = 0)
    {
        var converted = ConvertVelocity(fps, unit);
        var suffix = unit switch
        {
            VelocityUnit.FPS => " fps",
            VelocityUnit.MPS => " m/s",
            _ => " fps"
        };
        return $"{converted.ToString($"F{decimals}")}{suffix}";
    }

    #endregion

    #region Adjustment

    public double ConvertAdjustment(double mils, AdjustmentUnit targetUnit)
    {
        return targetUnit switch
        {
            AdjustmentUnit.MIL => mils,
            AdjustmentUnit.MOA => mils * MilToMoa,
            _ => mils
        };
    }

    public double ConvertAdjustmentToCanonical(double value, AdjustmentUnit sourceUnit)
    {
        return sourceUnit switch
        {
            AdjustmentUnit.MIL => value,
            AdjustmentUnit.MOA => value * MoaToMil,
            _ => value
        };
    }

    public string FormatAdjustment(double mils, AdjustmentUnit unit, int decimals = 1)
    {
        var converted = ConvertAdjustment(mils, unit);
        var suffix = unit switch
        {
            AdjustmentUnit.MIL => " MIL",
            AdjustmentUnit.MOA => " MOA",
            _ => " MIL"
        };
        return $"{converted.ToString($"F{decimals}")}{suffix}";
    }

    #endregion
}
