using System.Management.Automation;
using PowerPlug.Tests.Infrastructure;

namespace PowerPlug.Tests.Integration;

/// <summary>
/// Byname cmdlets need a live session: they call New-Alias and friends in the caller's runspace and read $PROFILE.
/// Each test class instance points $PROFILE at a scratch file and puts it back afterwards.
/// </summary>
[Collection(PowerShellTestGroup.Name)]
public class BynameIntegrationTests : IDisposable
{
    private readonly PowerShellFixture _ps;
    private readonly TempDirectory _temp = new();
    private readonly string _profile;
    private readonly object? _originalProfile;

    public BynameIntegrationTests(PowerShellFixture ps)
    {
        _ps = ps;
        _profile = _temp.Combine("profile.ps1");
        _originalProfile = ps.Runspace.SessionStateProxy.GetVariable("PROFILE");
        _ps.Invoke("$global:PROFILE = $ppProfile", "ppProfile", _profile).ShouldSucceed();
    }

    public void Dispose()
    {
        _ps.Reset();
        _ps.Runspace.SessionStateProxy.SetVariable("PROFILE", _originalProfile);
        _temp.Dispose();
    }

    private string Profile() => File.Exists(_profile) ? File.ReadAllText(_profile) : string.Empty;

    [Fact]
    public void NewByname_CreatesAliasAndWritesProfile()
    {
        var result = _ps.Invoke("New-Byname -Name pp_list -Value Get-ChildItem -Scope Global -PassThru").ShouldSucceed();

        var alias = result.Only<AliasInfo>();
        Assert.Equal("pp_list", alias.Name);
        Assert.Equal("Get-ChildItem", alias.Definition);
        Assert.Equal(["New-Alias -Name pp_list -Value Get-ChildItem -Option None -Scope Global"], File.ReadAllLines(_profile));
    }

    [Fact]
    public void NewByname_WritesOptionForceAndDescription()
    {
        _ps.Invoke("New-Byname pp_ro Get-Date -Scope Global -Option ReadOnly -Force -Description 'read only'").ShouldSucceed();

        Assert.Equal(
            ["New-Alias -Name pp_ro -Value Get-Date -Option ReadOnly -Scope Global -Force -Description 'read only'"],
            File.ReadAllLines(_profile));
        Assert.Equal("ReadOnly", _ps.Invoke("(Get-Alias pp_ro).Options.ToString()").Only<string>());
    }

    [Fact]
    public void NewByname_PersistsSessionFunctionWithMarker()
    {
        _ps.Invoke("function global:Get-PpFoo { 'foo' }").ShouldSucceed();
        _ps.Invoke("New-Byname pp_foo Get-PpFoo -Scope Global").ShouldSucceed();

        var lines = File.ReadAllLines(_profile);
        Assert.Equal(2, lines.Length);
        Assert.Matches(@"^function Get-PpFoo \{\s*'foo'\s*\} # PowerPlug Byname$", lines[0]);
        Assert.Equal("New-Alias -Name pp_foo -Value Get-PpFoo -Option None -Scope Global", lines[1]);
    }

    [Fact]
    public void NewByname_RejectsFunctionNameWithSpacesBeforeChangingSession()
    {
        _ps.Invoke("Set-Item 'function:global:Get Pp Spaced' { 'x' }").ShouldSucceed();
        try
        {
            var result = _ps.Invoke("New-Byname pp_spaced 'Get Pp Spaced' -Scope Global -ErrorAction Stop");

            Assert.Contains("spaces or quotes", result.ShouldFail(), StringComparison.Ordinal);
            Assert.Empty(_ps.Invoke("Get-Alias pp_spaced -ErrorAction SilentlyContinue").Results);
            Assert.False(File.Exists(_profile));
        }
        finally
        {
            _ps.Invoke("Remove-Item 'function:global:Get Pp Spaced'");
        }
    }

    [Fact]
    public void NewByname_DuplicateAliasIsATerminatingErrorAndProfileIsNotTouchedTwice()
    {
        _ps.Invoke("New-Byname pp_dup Get-Date -Scope Global").ShouldSucceed();
        var second = _ps.Invoke("New-Byname pp_dup Get-Date -Scope Global -ErrorAction Stop");

        second.ShouldFailWith<SessionStateException>();
        Assert.Equal(1, File.ReadAllLines(_profile).Count(l => l.Contains("-Name pp_dup", StringComparison.Ordinal)));
    }

    [Fact]
    public void NewByname_LocalScopeIsTheDefault()
    {
        _ps.Invoke("New-Byname pp_local Get-Date").ShouldSucceed();
        Assert.Contains("-Scope Local", Profile(), StringComparison.Ordinal);
    }

    [Fact]
    public void SetByname_ReplacesExistingProfileEntryAndUpdatesSession()
    {
        _ps.Invoke("New-Byname pp_set Get-Date -Scope Global").ShouldSucceed();
        var result = _ps.Invoke("Set-Byname pp_set Get-Location -Scope Global -PassThru").ShouldSucceed();

        Assert.Equal("Get-Location", result.Only<AliasInfo>().Definition);
        Assert.Equal(["Set-Alias -Name pp_set -Value Get-Location -Option None -Scope Global"], File.ReadAllLines(_profile));
        Assert.Equal("Get-Location", _ps.Invoke("(Get-Alias pp_set).Definition").Only<string>());
    }

    [Fact]
    public void SetByname_OnUnknownAliasCreatesItLikeSetAlias()
    {
        _ps.Invoke("Set-Byname pp_new Get-Date -Scope Global").ShouldSucceed();
        Assert.Equal("Get-Date", _ps.Invoke("(Get-Alias pp_new).Definition").Only<string>());
        Assert.StartsWith("Set-Alias -Name pp_new", Profile(), StringComparison.Ordinal);
    }

    [Fact]
    public void RemoveByname_RemovesAliasAndProfileLine()
    {
        _ps.Invoke("New-Byname pp_rm Get-Date -Scope Global").ShouldSucceed();
        var result = _ps.Invoke("Remove-Byname pp_rm -Scope Global -Verbose").ShouldSucceed();

        Assert.Equal(string.Empty, Profile());
        Assert.Empty(_ps.Invoke("Get-Alias pp_rm -ErrorAction SilentlyContinue").Results);
        Assert.Contains(result.Verbose, v => v.Contains("Removed 1 entry", StringComparison.Ordinal));
    }

    [Fact]
    public void RemoveByname_AcceptsAliasInfoFromPipeline()
    {
        _ps.Invoke("New-Byname pp_piped Get-Date -Scope Global").ShouldSucceed();
        _ps.Invoke("Get-Alias pp_piped | Remove-Byname -Scope Global").ShouldSucceed();

        Assert.Empty(_ps.Invoke("Get-Alias pp_piped -ErrorAction SilentlyContinue").Results);
        Assert.Equal(string.Empty, Profile());
    }

    [Fact]
    public void RemoveByname_WarnsWhenNothingInProfile()
    {
        _ps.Invoke("New-Alias -Name pp_orphan -Value Get-Date -Scope Global").ShouldSucceed();
        var result = _ps.Invoke("Remove-Byname pp_orphan -Scope Global").ShouldSucceed();
        Assert.Contains(result.Warnings, w => w.Contains("No Byname entries", StringComparison.Ordinal));
    }

    [Fact]
    public void RemoveByname_OnUnknownAliasIsATerminatingError()
    {
        var result = _ps.Invoke("Remove-Byname pp_never_existed -Scope Global -ErrorAction Stop");
        result.ShouldFail();
        Assert.False(File.Exists(_profile));
    }

    [Fact]
    public void RemoveByname_WhatIfLeavesEverything()
    {
        _ps.Invoke("New-Byname pp_keep Get-Date -Scope Global").ShouldSucceed();
        _ps.Invoke("Remove-Byname pp_keep -Scope Global -WhatIf").ShouldSucceed();

        Assert.Single(_ps.Invoke("Get-Alias pp_keep").Results);
        Assert.Contains("pp_keep", Profile(), StringComparison.Ordinal);
    }

    [Fact]
    public void NewByname_WhatIfTouchesNothing()
    {
        _ps.Invoke("New-Byname pp_whatif Get-Date -Scope Global -WhatIf").ShouldSucceed();
        Assert.False(File.Exists(_profile));
        Assert.Empty(_ps.Invoke("Get-Alias pp_whatif -ErrorAction SilentlyContinue").Results);
    }

    [Fact]
    public void MissingProfileVariableIsAClearTerminatingError()
    {
        _ps.Invoke("Remove-Variable PROFILE -Scope Global -Force").ShouldSucceed();
        try
        {
            var result = _ps.Invoke("New-Byname pp_noprofile Get-Date -Scope Global -ErrorAction Stop");
            var error = result.ShouldFailWith<SessionStateException>();
            Assert.Contains("$PROFILE", error.Message, StringComparison.Ordinal);
            Assert.Empty(_ps.Invoke("Get-Alias pp_noprofile -ErrorAction SilentlyContinue").Results);
        }
        finally
        {
            _ps.Invoke("$global:PROFILE = $ppProfile", "ppProfile", _profile);
        }
    }

    [Fact]
    public void ToString_ReturnsTheLineThatWouldBePersisted()
    {
        _ps.Invoke("New-Byname pp_str Get-Date -Scope Global -Description 'd'").ShouldSucceed();
        var text = _ps.Invoke("[PowerPlug.Cmdlets.Profile.NewBynameCmdlet]@{ Name = 'pp_str'; Value = 'Get-Date'; Scope = 'Global'; Description = 'd' } | ForEach-Object ToString").Only<string>();
        Assert.Equal(File.ReadAllLines(_profile)[0], text);

        var remove = _ps.Invoke("[PowerPlug.Cmdlets.Profile.RemoveBynameCmdlet]@{ Name = 'pp_str'; Force = $true } | ForEach-Object ToString").Only<string>();
        Assert.Equal("Remove-Alias -Name pp_str -Scope Local -Force", remove);
    }

    [Fact]
    public void ProfileWrittenByByname_LoadsCleanlyInAFreshScope()
    {
        _ps.Invoke("function global:Get-PpRound { param($x) $x * 2 }").ShouldSucceed();
        _ps.Invoke("New-Byname pp_round Get-PpRound -Scope Global -Description \"it's doubled\"").ShouldSucceed();
        _ps.Invoke("New-Byname pp_ls Get-ChildItem -Scope Global").ShouldSucceed();

        var fresh = _ps.Invoke("""
            Remove-Alias pp_round, pp_ls -Scope Global -Force
            Remove-Item function:global:Get-PpRound
            . $PROFILE
            (Get-Alias pp_round).Definition, (Get-Alias pp_ls).Definition, (pp_round 21)
            """).ShouldSucceed();

        Assert.Equal(["Get-PpRound", "Get-ChildItem", "42"], fresh.Results.Select(r => r.ToString()));
    }
}
