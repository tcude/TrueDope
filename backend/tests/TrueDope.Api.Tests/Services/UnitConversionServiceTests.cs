using FluentAssertions;
using TrueDope.Api.Data.Entities;
using TrueDope.Api.Services;

namespace TrueDope.Api.Tests.Services;

public class UnitConversionServiceTests
{
    private readonly UnitConversionService _service;

    public UnitConversionServiceTests()
    {
        _service = new UnitConversionService();
    }

    #region Distance Tests

    [Fact]
    public void ConvertDistance_YardsToYards_ReturnsUnchanged()
    {
        var result = _service.ConvertDistance(100, DistanceUnit.Yards);
        result.Should().Be(100);
    }

    [Fact]
    public void ConvertDistance_YardsToMeters_ConvertsCorrectly()
    {
        var result = _service.ConvertDistance(100, DistanceUnit.Meters);
        result.Should().BeApproximately(91.44, 0.01);
    }

    [Fact]
    public void ConvertDistanceToCanonical_MetersToYards_ConvertsCorrectly()
    {
        var result = _service.ConvertDistanceToCanonical(100, DistanceUnit.Meters);
        result.Should().BeApproximately(109.361, 0.01);
    }

    [Fact]
    public void ConvertDistance_RoundTrip_ReturnsOriginal()
    {
        const double original = 500;
        var meters = _service.ConvertDistance(original, DistanceUnit.Meters);
        var backToYards = _service.ConvertDistanceToCanonical(meters, DistanceUnit.Meters);
        backToYards.Should().BeApproximately(original, 0.01);
    }

    [Fact]
    public void FormatDistance_Yards_FormatsCorrectly()
    {
        var result = _service.FormatDistance(100, DistanceUnit.Yards);
        result.Should().Be("100 yd");
    }

    [Fact]
    public void FormatDistance_Meters_FormatsCorrectly()
    {
        var result = _service.FormatDistance(100, DistanceUnit.Meters);
        result.Should().Be("91 m");
    }

    #endregion

    #region Temperature Tests

    [Fact]
    public void ConvertTemperature_FahrenheitToFahrenheit_ReturnsUnchanged()
    {
        var result = _service.ConvertTemperature(72, TemperatureUnit.Fahrenheit);
        result.Should().Be(72);
    }

    [Fact]
    public void ConvertTemperature_FahrenheitToCelsius_ConvertsCorrectly()
    {
        var result = _service.ConvertTemperature(32, TemperatureUnit.Celsius);
        result.Should().BeApproximately(0, 0.01);
    }

    [Fact]
    public void ConvertTemperature_FahrenheitToCelsius_100F()
    {
        var result = _service.ConvertTemperature(100, TemperatureUnit.Celsius);
        result.Should().BeApproximately(37.78, 0.01);
    }

    [Fact]
    public void ConvertTemperatureToCanonical_CelsiusToFahrenheit_ConvertsCorrectly()
    {
        var result = _service.ConvertTemperatureToCanonical(0, TemperatureUnit.Celsius);
        result.Should().BeApproximately(32, 0.01);
    }

    [Fact]
    public void ConvertTemperature_RoundTrip_ReturnsOriginal()
    {
        const double original = 72;
        var celsius = _service.ConvertTemperature(original, TemperatureUnit.Celsius);
        var backToFahrenheit = _service.ConvertTemperatureToCanonical(celsius, TemperatureUnit.Celsius);
        backToFahrenheit.Should().BeApproximately(original, 0.01);
    }

    [Fact]
    public void FormatTemperature_Fahrenheit_FormatsCorrectly()
    {
        var result = _service.FormatTemperature(72, TemperatureUnit.Fahrenheit);
        result.Should().Be("72°F");
    }

    [Fact]
    public void FormatTemperature_Celsius_FormatsCorrectly()
    {
        var result = _service.FormatTemperature(68, TemperatureUnit.Celsius);
        result.Should().Be("20°C");
    }

    #endregion

    #region Pressure Tests

    [Fact]
    public void ConvertPressure_InHgToInHg_ReturnsUnchanged()
    {
        var result = _service.ConvertPressure(29.92, PressureUnit.InHg);
        result.Should().Be(29.92);
    }

    [Fact]
    public void ConvertPressure_InHgToHPa_ConvertsCorrectly()
    {
        var result = _service.ConvertPressure(29.92, PressureUnit.HPa);
        result.Should().BeApproximately(1013.25, 0.5);
    }

    [Fact]
    public void ConvertPressureToCanonical_HPaToInHg_ConvertsCorrectly()
    {
        var result = _service.ConvertPressureToCanonical(1013.25, PressureUnit.HPa);
        result.Should().BeApproximately(29.92, 0.01);
    }

    [Fact]
    public void ConvertPressure_RoundTrip_ReturnsOriginal()
    {
        const double original = 29.92;
        var hpa = _service.ConvertPressure(original, PressureUnit.HPa);
        var backToInHg = _service.ConvertPressureToCanonical(hpa, PressureUnit.HPa);
        backToInHg.Should().BeApproximately(original, 0.01);
    }

    [Fact]
    public void FormatPressure_InHg_FormatsCorrectly()
    {
        var result = _service.FormatPressure(29.92, PressureUnit.InHg);
        result.Should().Be("29.92 inHg");
    }

    [Fact]
    public void FormatPressure_HPa_FormatsCorrectly()
    {
        var result = _service.FormatPressure(29.92, PressureUnit.HPa);
        result.Should().Contain("hPa");
    }

    #endregion

    #region Velocity Tests

    [Fact]
    public void ConvertVelocity_FpsToFps_ReturnsUnchanged()
    {
        var result = _service.ConvertVelocity(2800, VelocityUnit.FPS);
        result.Should().Be(2800);
    }

    [Fact]
    public void ConvertVelocity_FpsToMps_ConvertsCorrectly()
    {
        var result = _service.ConvertVelocity(3280.84, VelocityUnit.MPS);
        result.Should().BeApproximately(1000, 0.1);
    }

    [Fact]
    public void ConvertVelocityToCanonical_MpsToFps_ConvertsCorrectly()
    {
        var result = _service.ConvertVelocityToCanonical(1000, VelocityUnit.MPS);
        result.Should().BeApproximately(3280.84, 0.1);
    }

    [Fact]
    public void ConvertVelocity_RoundTrip_ReturnsOriginal()
    {
        const double original = 2800;
        var mps = _service.ConvertVelocity(original, VelocityUnit.MPS);
        var backToFps = _service.ConvertVelocityToCanonical(mps, VelocityUnit.MPS);
        backToFps.Should().BeApproximately(original, 0.01);
    }

    [Fact]
    public void FormatVelocity_Fps_FormatsCorrectly()
    {
        var result = _service.FormatVelocity(2800, VelocityUnit.FPS);
        result.Should().Be("2800 fps");
    }

    [Fact]
    public void FormatVelocity_Mps_FormatsCorrectly()
    {
        var result = _service.FormatVelocity(3281, VelocityUnit.MPS);
        result.Should().Contain("m/s");
    }

    #endregion

    #region Adjustment Tests

    [Fact]
    public void ConvertAdjustment_MilToMil_ReturnsUnchanged()
    {
        var result = _service.ConvertAdjustment(2.5, AdjustmentUnit.MIL);
        result.Should().Be(2.5);
    }

    [Fact]
    public void ConvertAdjustment_MilToMoa_ConvertsCorrectly()
    {
        var result = _service.ConvertAdjustment(1, AdjustmentUnit.MOA);
        result.Should().BeApproximately(3.438, 0.01);
    }

    [Fact]
    public void ConvertAdjustmentToCanonical_MoaToMil_ConvertsCorrectly()
    {
        var result = _service.ConvertAdjustmentToCanonical(3.438, AdjustmentUnit.MOA);
        result.Should().BeApproximately(1, 0.01);
    }

    [Fact]
    public void ConvertAdjustment_RoundTrip_ReturnsOriginal()
    {
        const double original = 2.5;
        var moa = _service.ConvertAdjustment(original, AdjustmentUnit.MOA);
        var backToMil = _service.ConvertAdjustmentToCanonical(moa, AdjustmentUnit.MOA);
        backToMil.Should().BeApproximately(original, 0.01);
    }

    [Fact]
    public void FormatAdjustment_Mil_FormatsCorrectly()
    {
        var result = _service.FormatAdjustment(2.5, AdjustmentUnit.MIL);
        result.Should().Be("2.5 MIL");
    }

    [Fact]
    public void FormatAdjustment_Moa_FormatsCorrectly()
    {
        var result = _service.FormatAdjustment(1, AdjustmentUnit.MOA);
        result.Should().Contain("MOA");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ConvertDistance_Zero_ReturnsZero()
    {
        _service.ConvertDistance(0, DistanceUnit.Meters).Should().Be(0);
    }

    [Fact]
    public void ConvertTemperature_AbsoluteZeroFahrenheit_ConvertsCorrectly()
    {
        // Absolute zero is -459.67°F = -273.15°C
        var result = _service.ConvertTemperature(-459.67, TemperatureUnit.Celsius);
        result.Should().BeApproximately(-273.15, 0.1);
    }

    [Fact]
    public void ConvertVelocity_LargeValue_ConvertsCorrectly()
    {
        var result = _service.ConvertVelocity(10000, VelocityUnit.MPS);
        result.Should().BeApproximately(3048, 1);
    }

    [Fact]
    public void ConvertAdjustment_NegativeValue_ConvertsCorrectly()
    {
        var result = _service.ConvertAdjustment(-2.0, AdjustmentUnit.MOA);
        result.Should().BeApproximately(-6.876, 0.01);
    }

    #endregion
}
