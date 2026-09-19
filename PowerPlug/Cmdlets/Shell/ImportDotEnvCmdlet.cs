using System.Management.Automation;
using PowerPlug.Base;
using PowerPlug.Internal;
using PowerPlug.Models;

namespace PowerPlug.Cmdlets.Shell;

/// <summary>
/// <para type="synopsis">Loads variables from a .env file into the current session.</para>
/// <para type="description">Reads KEY=VALUE pairs from a dotenv file and sets them as environment variables for the
/// current process, so they are visible through $env: and to child processes. Existing variables are left alone
/// unless -Force is given. Supports comments, "export " prefixes, quoted and multi line values, and optional
/// ${VAR} expansion with -Expand.</para>
/// <example>
/// <para>Load ./.env</para>
/// <code>Import-DotEnv</code>
/// </example>
/// <example>
/// <para>Load a specific file, override existing values and show what was set</para>
/// <code>Import-DotEnv ./config/.env.local -Force -PassThru</code>
/// </example>
/// </summary>
[Cmdlet(VerbsData.Import, "DotEnv", SupportsShouldProcess = true)]
[Alias("dotenv")]
[OutputType(typeof(DotEnvEntry))]
public sealed class ImportDotEnvCmdlet : PowerPlugCmdlet
{
    /// <summary>
    /// <para type="description">Path to the .env file. Defaults to .env in the current directory.</para>
    /// </summary>
    [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
    [Alias("FullName")]
    [ValidateNotNullOrEmpty]
    public string[] Path { get; set; } = [".env"];

    /// <summary>
    /// <para type="description">Overwrite variables that already exist.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Force { get; set; }

    /// <summary>
    /// <para type="description">Expand ${VAR} and $VAR references in values.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter Expand { get; set; }

    /// <summary>
    /// <para type="description">Return an entry for each variable that was set.</para>
    /// </summary>
    [Parameter]
    public SwitchParameter PassThru { get; set; }

    /// <inheritdoc />
    protected override void ProcessRecord()
    {
        foreach (var path in Path)
        {
            var resolved = ResolvePath(path);
            if (!File.Exists(resolved))
            {
                WriteFileError(new FileNotFoundException($"File not found: {resolved}"), resolved);
                continue;
            }

            List<KeyValuePair<string, string>> pairs;
            try
            {
                pairs = DotEnvParser.Parse(File.ReadAllText(resolved), Expand);
            }
            catch (FormatException ex)
            {
                WriteError(ex, "InvalidDotEnv", ErrorCategory.InvalidData, resolved);
                continue;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                WriteFileError(ex, resolved);
                continue;
            }

            var set = 0;
            foreach (var (key, value) in pairs)
            {
                var existing = Environment.GetEnvironmentVariable(key);
                if (existing is not null && !Force)
                {
                    WriteVerbose($"Skipped {key}: already set. Use -Force to overwrite.");
                    continue;
                }

                if (!ShouldProcess(key, "Set environment variable"))
                {
                    continue;
                }

                Environment.SetEnvironmentVariable(key, value);
                set++;

                if (PassThru)
                {
                    WriteObject(new DotEnvEntry { Name = key, Value = value, Overwritten = existing is not null });
                }
            }

            WriteVerbose($"Loaded {set} of {pairs.Count} variable(s) from {resolved}");
        }
    }
}
