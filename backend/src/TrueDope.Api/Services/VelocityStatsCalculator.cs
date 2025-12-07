namespace TrueDope.Api.Services;

public class VelocityStats
{
    public int Count { get; set; }
    public decimal? Average { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public decimal? ExtremeSpread { get; set; }
    public decimal? StandardDeviation { get; set; }
}

public static class VelocityStatsCalculator
{
    public static VelocityStats Calculate(IEnumerable<decimal> velocities)
    {
        var list = velocities.ToList();

        if (!list.Any())
        {
            return new VelocityStats { Count = 0 };
        }

        var count = list.Count;
        var average = list.Average();
        var high = list.Max();
        var low = list.Min();
        var extremeSpread = high - low;

        // Standard deviation calculation (population SD)
        var sumOfSquares = list.Sum(v => (v - average) * (v - average));
        var variance = sumOfSquares / count;
        var standardDeviation = (decimal)Math.Sqrt((double)variance);

        return new VelocityStats
        {
            Count = count,
            Average = Math.Round(average, 1),
            High = Math.Round(high, 1),
            Low = Math.Round(low, 1),
            ExtremeSpread = Math.Round(extremeSpread, 1),
            StandardDeviation = Math.Round(standardDeviation, 2)
        };
    }
}
