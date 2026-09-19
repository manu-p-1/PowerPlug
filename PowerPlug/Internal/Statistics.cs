namespace PowerPlug.Internal;

/// <summary>
/// Small descriptive statistics used by the benchmarking and latency cmdlets.
/// </summary>
internal static class Statistics
{
    /// <summary>
    /// Median of a sorted sample. The input must already be sorted ascending.
    /// </summary>
    public static double MedianOfSorted(IReadOnlyList<double> sorted)
    {
        if (sorted.Count == 0)
        {
            return 0;
        }

        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }

    /// <summary>
    /// Population standard deviation.
    /// </summary>
    public static double StandardDeviation(IReadOnlyList<double> values, double mean)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        double sumOfSquares = 0;
        foreach (var value in values)
        {
            var delta = value - mean;
            sumOfSquares += delta * delta;
        }

        return Math.Sqrt(sumOfSquares / values.Count);
    }

    /// <summary>
    /// Mean absolute difference between consecutive samples, which is how network tools report jitter.
    /// </summary>
    public static double Jitter(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        double total = 0;
        for (var i = 1; i < values.Count; i++)
        {
            total += Math.Abs(values[i] - values[i - 1]);
        }

        return total / (values.Count - 1);
    }
}
