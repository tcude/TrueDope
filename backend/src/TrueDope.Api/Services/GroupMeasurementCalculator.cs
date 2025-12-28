namespace TrueDope.Api.Services;

public class GroupMetrics
{
    // All values in inches unless noted
    public decimal ExtremeSpread { get; set; }
    public decimal MeanRadius { get; set; }
    public decimal HorizontalSpread { get; set; }
    public decimal VerticalSpread { get; set; }
    public decimal RadialStdDev { get; set; }
    public decimal HorizontalStdDev { get; set; }
    public decimal VerticalStdDev { get; set; }
    public decimal Cep50 { get; set; }
    public decimal CentroidX { get; set; }
    public decimal CentroidY { get; set; }
}

public interface IGroupMeasurementCalculator
{
    /// <summary>
    /// Calculate all group metrics from hole positions.
    /// </summary>
    /// <param name="holePositions">Array of (X, Y) positions in inches relative to POA</param>
    /// <param name="bulletDiameter">Bullet diameter in inches for edge-to-edge calculation</param>
    /// <returns>Calculated metrics</returns>
    GroupMetrics Calculate(IReadOnlyList<(decimal X, decimal Y)> holePositions, decimal bulletDiameter);

    /// <summary>
    /// Convert inches to MOA at a given distance.
    /// </summary>
    decimal InchesToMoa(decimal inches, int distanceYards);
}

public class GroupMeasurementCalculator : IGroupMeasurementCalculator
{
    public GroupMetrics Calculate(IReadOnlyList<(decimal X, decimal Y)> holePositions, decimal bulletDiameter)
    {
        if (holePositions.Count < 2)
            throw new ArgumentException("At least 2 hole positions are required", nameof(holePositions));

        // ==================== Centroid (POI) ====================
        var centroidX = holePositions.Average(h => h.X);
        var centroidY = holePositions.Average(h => h.Y);

        // ==================== Extreme Spread ====================
        // Maximum center-to-center distance + bullet diameter (edge-to-edge)
        decimal maxCenterToCenter = 0;
        for (int i = 0; i < holePositions.Count; i++)
        {
            for (int j = i + 1; j < holePositions.Count; j++)
            {
                var dist = Distance(holePositions[i], holePositions[j]);
                if (dist > maxCenterToCenter)
                    maxCenterToCenter = dist;
            }
        }
        var extremeSpread = maxCenterToCenter + bulletDiameter;

        // ==================== Mean Radius ====================
        // Average distance from centroid
        var radii = holePositions.Select(h => Distance(h, (centroidX, centroidY))).ToList();
        var meanRadius = radii.Average();

        // ==================== H/V Spread ====================
        var horizontalSpread = holePositions.Max(h => h.X) - holePositions.Min(h => h.X) + bulletDiameter;
        var verticalSpread = holePositions.Max(h => h.Y) - holePositions.Min(h => h.Y) + bulletDiameter;

        // ==================== Standard Deviations ====================
        // Using population standard deviation (divide by N, not N-1)
        var xValues = holePositions.Select(h => (double)h.X).ToList();
        var yValues = holePositions.Select(h => (double)h.Y).ToList();
        var radiiDouble = radii.Select(r => (double)r).ToList();

        var horizontalStdDev = (decimal)PopulationStdDev(xValues);
        var verticalStdDev = (decimal)PopulationStdDev(yValues);
        var radialStdDev = (decimal)PopulationStdDev(radiiDouble);

        // ==================== CEP50 ====================
        // Circular Error Probable - radius containing 50% of shots
        // For small shot counts, use actual median radial distance
        var sortedRadii = radii.OrderBy(r => r).ToList();
        decimal cep50;
        if (sortedRadii.Count >= 2)
        {
            // Use actual median for better accuracy with small groups
            int midIndex = sortedRadii.Count / 2;
            cep50 = sortedRadii.Count % 2 == 0
                ? (sortedRadii[midIndex - 1] + sortedRadii[midIndex]) / 2
                : sortedRadii[midIndex];
        }
        else
        {
            // Fallback to approximation for edge cases
            cep50 = 0.5887m * (horizontalStdDev + verticalStdDev);
        }

        return new GroupMetrics
        {
            ExtremeSpread = Math.Round(extremeSpread, 4),
            MeanRadius = Math.Round(meanRadius, 4),
            HorizontalSpread = Math.Round(horizontalSpread, 4),
            VerticalSpread = Math.Round(verticalSpread, 4),
            RadialStdDev = Math.Round(radialStdDev, 5),
            HorizontalStdDev = Math.Round(horizontalStdDev, 5),
            VerticalStdDev = Math.Round(verticalStdDev, 5),
            Cep50 = Math.Round(cep50, 4),
            CentroidX = Math.Round(centroidX, 4),
            CentroidY = Math.Round(centroidY, 4)
        };
    }

    public decimal InchesToMoa(decimal inches, int distanceYards)
    {
        if (distanceYards <= 0)
            throw new ArgumentException("Distance must be positive", nameof(distanceYards));

        // 1 MOA = 1.047 inches at 100 yards
        // MOA = (inches / distance) × (100 / 1.047)
        return Math.Round((inches / distanceYards) * (100m / 1.047m), 3);
    }

    private static decimal Distance((decimal X, decimal Y) a, (decimal X, decimal Y) b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return (decimal)Math.Sqrt((double)(dx * dx + dy * dy));
    }

    private static double PopulationStdDev(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return 0;

        var avg = values.Average();
        var sumOfSquares = values.Sum(v => Math.Pow(v - avg, 2));
        var variance = sumOfSquares / values.Count; // Population variance (N, not N-1)
        return Math.Sqrt(variance);
    }
}
