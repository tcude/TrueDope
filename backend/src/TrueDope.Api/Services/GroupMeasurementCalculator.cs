namespace TrueDope.Api.Services;

public class GroupMetrics
{
    // All values in inches unless noted

    /// <summary>
    /// Extreme Spread - Center-to-Center (CTC).
    /// Distance between the centers of the two farthest holes.
    /// This is what most shooters compare when discussing group size.
    /// </summary>
    public decimal ExtremeSpreadCtc { get; set; }

    /// <summary>
    /// Extreme Spread - Edge-to-Edge (ETE).
    /// CTC + bullet diameter. The actual physical size of the group on paper.
    /// </summary>
    public decimal ExtremeSpreadEte { get; set; }

    public decimal MeanRadius { get; set; }

    /// <summary>
    /// Horizontal Spread - Center-to-Center
    /// </summary>
    public decimal HorizontalSpreadCtc { get; set; }

    /// <summary>
    /// Horizontal Spread - Edge-to-Edge (CTC + bullet diameter)
    /// </summary>
    public decimal HorizontalSpreadEte { get; set; }

    /// <summary>
    /// Vertical Spread - Center-to-Center
    /// </summary>
    public decimal VerticalSpreadCtc { get; set; }

    /// <summary>
    /// Vertical Spread - Edge-to-Edge (CTC + bullet diameter)
    /// </summary>
    public decimal VerticalSpreadEte { get; set; }

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
        // CTC = Center-to-Center (what most shooters compare)
        // ETE = Edge-to-Edge (CTC + bullet diameter, actual physical size)
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
        var extremeSpreadCtc = maxCenterToCenter;
        var extremeSpreadEte = maxCenterToCenter + bulletDiameter;

        // ==================== Mean Radius ====================
        // Average distance from centroid
        var radii = holePositions.Select(h => Distance(h, (centroidX, centroidY))).ToList();
        var meanRadius = radii.Average();

        // ==================== H/V Spread ====================
        var horizontalSpreadCtc = holePositions.Max(h => h.X) - holePositions.Min(h => h.X);
        var horizontalSpreadEte = horizontalSpreadCtc + bulletDiameter;
        var verticalSpreadCtc = holePositions.Max(h => h.Y) - holePositions.Min(h => h.Y);
        var verticalSpreadEte = verticalSpreadCtc + bulletDiameter;

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
            ExtremeSpreadCtc = Math.Round(extremeSpreadCtc, 4),
            ExtremeSpreadEte = Math.Round(extremeSpreadEte, 4),
            MeanRadius = Math.Round(meanRadius, 4),
            HorizontalSpreadCtc = Math.Round(horizontalSpreadCtc, 4),
            HorizontalSpreadEte = Math.Round(horizontalSpreadEte, 4),
            VerticalSpreadCtc = Math.Round(verticalSpreadCtc, 4),
            VerticalSpreadEte = Math.Round(verticalSpreadEte, 4),
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
