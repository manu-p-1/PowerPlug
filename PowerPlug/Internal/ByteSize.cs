using System.Globalization;

namespace PowerPlug.Internal;

/// <summary>
/// Formats byte counts the way people expect to read them.
/// </summary>
internal static class ByteSize
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];

    /// <summary>
    /// Formats a byte count using 1024 based units, for example "1.5 MB".
    /// </summary>
    public static string Format(long bytes)
    {
        if (bytes < 0)
        {
            return "-" + Format(bytes == long.MinValue ? long.MaxValue : -bytes);
        }

        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        var text = unit == 0
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);

        return $"{text} {Units[unit]}";
    }
}
