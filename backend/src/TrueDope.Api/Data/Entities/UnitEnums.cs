namespace TrueDope.Api.Data.Entities;

public enum DistanceUnit
{
    Yards = 0,
    Meters = 1
}

public enum AdjustmentUnit
{
    MIL = 0,
    MOA = 1
}

public enum TemperatureUnit
{
    Fahrenheit = 0,
    Celsius = 1
}

public enum PressureUnit
{
    InHg = 0,
    HPa = 1
}

public enum VelocityUnit
{
    FPS = 0,
    MPS = 1
}

public enum ThemePreference
{
    System = 0,
    Light = 1,
    Dark = 2
}

/// <summary>
/// How group size (extreme spread) is measured.
/// CTC = Center-to-Center (distance between farthest hole centers)
/// ETE = Edge-to-Edge (CTC + bullet diameter, actual physical group size)
/// </summary>
public enum GroupSizeMethod
{
    CenterToCenter = 0,  // Default - matches common apps like Ballistic-X
    EdgeToEdge = 1
}
