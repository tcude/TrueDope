using FluentAssertions;
using TrueDope.Api.Services;

namespace TrueDope.Api.Tests.Services;

public class VelocityStatsCalculatorTests
{
    [Fact]
    public void Calculate_WithValidVelocities_ShouldReturnCorrectStats()
    {
        // Arrange
        var velocities = new List<decimal> { 2800m, 2810m, 2795m, 2805m, 2790m };

        // Act
        var stats = VelocityStatsCalculator.Calculate(velocities);

        // Assert
        stats.Count.Should().Be(5);
        stats.Average.Should().Be(2800m);
        stats.High.Should().Be(2810m);
        stats.Low.Should().Be(2790m);
        stats.ExtremeSpread.Should().Be(20m);
    }

    [Fact]
    public void Calculate_ShouldCalculateStandardDeviationCorrectly()
    {
        // Arrange - known values for easy verification
        var velocities = new List<decimal> { 2800m, 2800m, 2800m, 2800m, 2800m };

        // Act
        var stats = VelocityStatsCalculator.Calculate(velocities);

        // Assert - all same values = 0 SD
        stats.StandardDeviation.Should().Be(0m);
    }

    [Fact]
    public void Calculate_WithVariedVelocities_ShouldHaveNonZeroStandardDeviation()
    {
        // Arrange
        var velocities = new List<decimal> { 2750m, 2800m, 2850m };

        // Act
        var stats = VelocityStatsCalculator.Calculate(velocities);

        // Assert
        stats.StandardDeviation.Should().BeGreaterThan(0);
        stats.StandardDeviation.Should().BeApproximately(40.82m, 1m); // ~40.82 SD
    }

    [Fact]
    public void Calculate_WithEmptyList_ShouldReturnNullStats()
    {
        // Arrange
        var velocities = new List<decimal>();

        // Act
        var stats = VelocityStatsCalculator.Calculate(velocities);

        // Assert - Empty list returns Count=0, all stats null (undefined for empty set)
        stats.Count.Should().Be(0);
        stats.Average.Should().BeNull();
        stats.High.Should().BeNull();
        stats.Low.Should().BeNull();
        stats.ExtremeSpread.Should().BeNull();
        stats.StandardDeviation.Should().BeNull();
    }

    [Fact]
    public void Calculate_WithSingleVelocity_ShouldHandleCorrectly()
    {
        // Arrange
        var velocities = new List<decimal> { 2800m };

        // Act
        var stats = VelocityStatsCalculator.Calculate(velocities);

        // Assert
        stats.Count.Should().Be(1);
        stats.Average.Should().Be(2800m);
        stats.High.Should().Be(2800m);
        stats.Low.Should().Be(2800m);
        stats.ExtremeSpread.Should().Be(0m);
        stats.StandardDeviation.Should().Be(0m);
    }

    [Fact]
    public void Calculate_WithTwoVelocities_ShouldCalculateCorrectly()
    {
        // Arrange
        var velocities = new List<decimal> { 2800m, 2820m };

        // Act
        var stats = VelocityStatsCalculator.Calculate(velocities);

        // Assert
        stats.Count.Should().Be(2);
        stats.Average.Should().Be(2810m);
        stats.High.Should().Be(2820m);
        stats.Low.Should().Be(2800m);
        stats.ExtremeSpread.Should().Be(20m);
    }

    [Fact]
    public void Calculate_ShouldRoundToTwoDecimalPlaces()
    {
        // Arrange - values that would produce non-round results
        var velocities = new List<decimal> { 2801m, 2802m, 2803m };

        // Act
        var stats = VelocityStatsCalculator.Calculate(velocities);

        // Assert
        stats.Average.Should().Be(2802m);
        // SD should be rounded
        var sdString = stats.StandardDeviation.ToString();
        var decimalPlaces = sdString.Contains('.')
            ? sdString.Split('.')[1].Length
            : 0;
        decimalPlaces.Should().BeLessOrEqualTo(2);
    }
}
