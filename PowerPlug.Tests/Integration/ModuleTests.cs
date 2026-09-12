using System.Management.Automation;
using PowerPlug.Tests.Infrastructure;

namespace PowerPlug.Tests.Integration;

/// <summary>
/// Checks that the module loads as a whole and that what the manifest promises matches what the assembly provides.
/// </summary>
[Collection(PowerShellTestGroup.Name)]
public class ModuleTests(PowerShellFixture ps)
{
    [Fact]
    public void ManifestIsValid()
    {
        ps.Invoke("Test-ModuleManifest -Path $manifest -ErrorAction Stop", "manifest", PowerShellFixture.ManifestPath).ShouldSucceed();
    }

    [Fact]
    public void EveryCmdletInAssemblyIsExported()
    {
        var expected = typeof(Base.PowerPlugCmdlet).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(PSCmdlet)))
            .Select(t => t.GetCustomAttributes(typeof(CmdletAttribute), false).Cast<CmdletAttribute>().Single())
            .Select(a => $"{a.VerbName}-{a.NounName}")
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var exported = ps.Invoke("(Get-Module PowerPlug).ExportedCmdlets.Keys").ShouldSucceed()
            .Results.Select(r => r.ToString()).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray();

        Assert.Equal(expected, exported);
    }

    [Fact]
    public void EveryAliasInAssemblyIsExported()
    {
        var expected = typeof(Base.PowerPlugCmdlet).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(PSCmdlet)))
            .SelectMany(t => t.GetCustomAttributes(typeof(AliasAttribute), false).Cast<AliasAttribute>())
            .SelectMany(a => a.AliasNames)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var exported = ps.Invoke("(Get-Module PowerPlug).ExportedAliases.Keys").ShouldSucceed()
            .Results.Select(r => r.ToString()).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray();

        Assert.Equal(expected, exported);
    }

    [Fact]
    public void AllCmdletsUseApprovedVerbs()
    {
        var approved = ps.Invoke("(Get-Verb).Verb").ShouldSucceed().Results.Select(r => r.ToString()).ToHashSet(StringComparer.Ordinal);
        var verbs = ps.Invoke("(Get-Command -Module PowerPlug -CommandType Cmdlet).Verb").ShouldSucceed().Results.Select(r => r.ToString());
        Assert.All(verbs, v => Assert.Contains(v, approved));
    }

    [Fact]
    public void AllCmdletsHaveHelp()
    {
        var result = ps.Invoke("""
            Get-Command -Module PowerPlug -CommandType Cmdlet | ForEach-Object {
                $help = Get-Help $_.Name -ErrorAction SilentlyContinue
                [pscustomobject]@{ Name = $_.Name; Synopsis = $help.Synopsis; HasExamples = [bool]$help.examples }
            }
            """).ShouldSucceed();

        foreach (var row in result.Results)
        {
            var name = row.Properties["Name"].Value.ToString();
            var synopsis = row.Properties["Synopsis"].Value?.ToString();
            Assert.False(string.IsNullOrWhiteSpace(synopsis), $"{name} has no synopsis");
            Assert.False(synopsis!.TrimStart().StartsWith(name!, StringComparison.Ordinal), $"{name} only has auto generated syntax help");
            Assert.True((bool)row.Properties["HasExamples"].Value, $"{name} has no examples");
        }
    }

    [Fact]
    public void FormatFileCoversEveryTableWorthyOutputType()
    {
        var formatted = ps.Invoke("Get-FormatData -TypeName 'PowerPlug.Models.*' | ForEach-Object TypeNames").ShouldSucceed()
            .Results.Select(r => r.ToString()).ToHashSet(StringComparer.Ordinal);

        string[] expected =
        [
            "PowerPlug.Models.PathEntry", "PowerPlug.Models.PortTestResult", "PowerPlug.Models.ListeningPort",
            "PowerPlug.Models.UrlTestResult", "PowerPlug.Models.TlsCertificateInfo", "PowerPlug.Models.DirectorySizeInfo",
            "PowerPlug.Models.RenameResult", "PowerPlug.Models.ColorInfo", "PowerPlug.Models.BenchmarkResult",
            "PowerPlug.Models.HashComparisonResult", "PowerPlug.Models.FileEncodingInfo", "PowerPlug.Models.LineEndingResult",
            "PowerPlug.Models.FileSizeInfo", "PowerPlug.Models.DirectoryComparison",
        ];

        foreach (var type in expected)
        {
            Assert.Contains(type, formatted);
        }
    }

    [Fact]
    public void ExperimentalCmdletsWarnOnce()
    {
        var result = ps.Invoke("Get-ListeningPort -NoProcess -Port 1 | Out-Null").ShouldSucceed();
        var warning = Assert.Single(result.ExperimentalWarnings);
        Assert.StartsWith("Get-ListeningPort is experimental.", warning, StringComparison.Ordinal);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void StableCmdletsDoNotWarn()
    {
        var result = ps.Invoke("'x' | ConvertTo-Base64 | Out-Null").ShouldSucceed();
        Assert.Empty(result.Warnings);
        Assert.Empty(result.ExperimentalWarnings);
    }
}
