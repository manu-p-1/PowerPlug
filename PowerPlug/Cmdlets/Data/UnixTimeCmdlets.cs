using System.Management.Automation;
using PowerPlug.Base;

namespace PowerPlug.Cmdlets.Data;

/// <summary>
/// <para type="synopsis">Converts a Unix timestamp to a DateTime.</para>
/// <para type="description">Accepts seconds or milliseconds since 1970-01-01 UTC. The unit is detected
/// automatically (values above 99,999,999,999 are treated as milliseconds) unless -Unit is given. The result
/// is in local time unless -Utc is specified.</para>
/// <example>
/// <para>Convert a timestamp from an API response</para>
/// <code>1700000000 | ConvertFrom-UnixTime</code>
/// </example>
/// <example>
/// <para>Force millisecond interpretation and keep UTC</para>
/// <code>ConvertFrom-UnixTime 1700000000123 -Unit Milliseconds -Utc</code>
/// </example>
/// </summary>
[Cmdlet(VerbsData.ConvertFrom, "UnixTime")]
[Alias("fromepoch")]
[OutputType(typeof(DateTime))]
public sealed class ConvertFromUnixTimeCmdlet : PowerPlugCmdlet
{
    private const long MillisecondThreshold = 99_999_999_999;

    /// <summary>
    /// <para type="description">Seconds or milliseconds since the Unix epoch.</para>
    /// </summary>
    [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
    public long Timestamp { get; set; }

    /// <summary>
    /// <para type="description">How to interpret the timestamp. Defaults to Auto.</para>
    /// </summary>
    [Parameter]
    [ValidateSet("Auto", "Seconds", "Milliseconds")]
    public string Unit { get; set; } = "Auto";

    /// <summary>
    /// <para type="description">Return the time in UTC instead of local time.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Utc { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var milliseconds = Unit switch
        {
            "Seconds" => false,
            "Milliseconds" => true,
            _ => Timestamp > MillisecondThreshold || Timestamp < -MillisecondThreshold,
        };

        DateTimeOffset value;
        try
        {
            value = milliseconds
                ? DateTimeOffset.FromUnixTimeMilliseconds(Timestamp)
                : DateTimeOffset.FromUnixTimeSeconds(Timestamp);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            WriteError(ex, "TimestampOutOfRange", ErrorCategory.InvalidArgument, Timestamp);
            return;
        }

        WriteObject(Utc ? value.UtcDateTime : value.LocalDateTime);
    }
}

/// <summary>
/// <para type="synopsis">Converts a DateTime to a Unix timestamp.</para>
/// <para type="description">Returns the number of seconds (or milliseconds with -Milliseconds) since
/// 1970-01-01 UTC. Defaults to the current time when no date is supplied. DateTime values with an
/// unspecified kind are treated as local time, matching Get-Date.</para>
/// <example>
/// <para>Current time as epoch seconds</para>
/// <code>ConvertTo-UnixTime</code>
/// </example>
/// <example>
/// <para>A specific date in milliseconds</para>
/// <code>Get-Date "2024-01-01" | ConvertTo-UnixTime -Milliseconds</code>
/// </example>
/// </summary>
[Cmdlet(VerbsData.ConvertTo, "UnixTime")]
[Alias("toepoch")]
[OutputType(typeof(long))]
public sealed class ConvertToUnixTimeCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">The date to convert. Defaults to now.</para>
    /// </summary>
    [Parameter(Position = 0, ValueFromPipeline = true)]
    public DateTime? Date { get; set; }

    /// <summary>
    /// <para type="description">Return milliseconds instead of seconds.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Milliseconds { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        var date = Date ?? DateTime.Now;
        if (date.Kind == DateTimeKind.Unspecified)
        {
            date = DateTime.SpecifyKind(date, DateTimeKind.Local);
        }

        var offset = new DateTimeOffset(date);
        WriteObject(Milliseconds ? offset.ToUnixTimeMilliseconds() : offset.ToUnixTimeSeconds());
    }
}
