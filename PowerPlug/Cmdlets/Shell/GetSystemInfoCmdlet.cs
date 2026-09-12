using System.Collections;
using System.Management.Automation;
using System.Runtime.InteropServices;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Shell;

/// <summary>
/// <para type="synopsis">Shows a one screen summary of the machine and session.</para>
/// <para type="description">Collects host name, OS and version, architecture, CPU count, memory available to the
/// runtime, uptime, .NET runtime, PowerShell version, PowerPlug version, elevation and time zone.</para>
/// <example>
/// <para>Show the summary</para>
/// <code>Get-SystemInfo</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "SystemInfo")]
[Alias("sysinfo")]
[OutputType(typeof(SystemInfo))]
public sealed class GetSystemInfoCmdlet : PowerPlugCmdlet
{
    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        long? totalMemory = null;
        try
        {
            var memory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            if (memory > 0)
            {
                totalMemory = memory;
            }
        }
        catch (InvalidOperationException)
        {
            // No GC info before the first collection on some runtimes.
        }

        WriteObject(new SystemInfo
        {
            ComputerName = Environment.MachineName,
            UserName = Environment.UserName,
            OS = RuntimeInformation.OSDescription.Trim(),
            OSVersion = Environment.OSVersion.VersionString,
            Architecture = $"{RuntimeInformation.OSArchitecture} (process: {RuntimeInformation.ProcessArchitecture})",
            ProcessorCount = Environment.ProcessorCount,
            TotalMemoryBytes = totalMemory,
            TotalMemory = totalMemory is null ? null : ByteSize.Format(totalMemory.Value),
            Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
            DotNetRuntime = RuntimeInformation.FrameworkDescription,
            PowerShellVersion = ReadPowerShellVersion(),
            PowerPlugVersion = ModuleInfo.Version,
            IsElevated = Elevation.IsElevated(),
            TimeZone = TimeZoneInfo.Local.Id,
        });
    }

    private string ReadPowerShellVersion()
    {
        if (SessionState is null)
        {
            return "unknown";
        }

        var table = SessionState.PSVariable.GetValue("PSVersionTable");
        var raw = table is PSObject pso ? pso.BaseObject : table;
        return raw is IDictionary dictionary && dictionary["PSVersion"] is { } version
            ? version.ToString() ?? "unknown"
            : "unknown";
    }
}
