using System.Management.Automation;
using PowerPlug.Internal;
using PowerPlug.Tests.Infrastructure;

namespace PowerPlug.Tests.Internal;

public class ProfileAliasWriterTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly string _profile;

    public ProfileAliasWriterTests()
    {
        _profile = _temp.Combine("profile.ps1");
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Constructor_RejectsBlankPath()
    {
        Assert.Throws<ArgumentException>(() => new ProfileAliasWriter(" "));
    }

    [Fact]
    public void EnsureExists_CreatesFileAndFolder()
    {
        var nested = _temp.Combine(Path.Combine("deep", "er", "profile.ps1"));
        var writer = new ProfileAliasWriter(nested);

        Assert.False(writer.Exists);
        writer.EnsureExists();
        Assert.True(writer.Exists);
        Assert.Equal(string.Empty, File.ReadAllText(nested));
    }

    [Fact]
    public void Append_WritesAliasLineAndCreatesProfile()
    {
        var writer = new ProfileAliasWriter(_profile);
        writer.Append("New-Alias -Name ll -Value Get-ChildItem -Option None -Scope Local");

        var lines = File.ReadAllLines(_profile);
        Assert.Equal(["New-Alias -Name ll -Value Get-ChildItem -Option None -Scope Local"], lines);
    }

    [Fact]
    public void Append_PreservesExistingContentAndAddsSingleTrailingNewline()
    {
        File.WriteAllText(_profile, "Write-Host hi\n\n\n");
        var writer = new ProfileAliasWriter(_profile);

        writer.Append("New-Alias -Name a -Value b -Option None -Scope Local");
        writer.Append("New-Alias -Name c -Value d -Option None -Scope Local");

        var text = File.ReadAllText(_profile);
        Assert.Equal("Write-Host hi" + Environment.NewLine
            + "New-Alias -Name a -Value b -Option None -Scope Local" + Environment.NewLine
            + "New-Alias -Name c -Value d -Option None -Scope Local" + Environment.NewLine, text);
    }

    [Fact]
    public void Append_WritesFunctionBodyOnceOnly()
    {
        var writer = new ProfileAliasWriter(_profile);

        writer.Append("New-Alias -Name f1 -Value Get-Foo -Option None -Scope Local", "Get-Foo", " 'foo' ");
        writer.Append("New-Alias -Name f2 -Value Get-Foo -Option None -Scope Local", "Get-Foo", " 'foo' ");

        var text = File.ReadAllText(_profile);
        Assert.Equal(1, CountOccurrences(text, "function Get-Foo { 'foo' } " + ProfileAliasWriter.FunctionMarker));
        Assert.Contains("-Name f1", text, StringComparison.Ordinal);
        Assert.Contains("-Name f2", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Append_DoesNotDuplicateHandWrittenFunction()
    {
        File.WriteAllText(_profile, "function Get-Foo\n{\n  'mine'\n}\n");
        var writer = new ProfileAliasWriter(_profile);

        writer.Append("New-Alias -Name f -Value Get-Foo -Option None -Scope Local", "Get-Foo", "'theirs'");

        var text = File.ReadAllText(_profile);
        Assert.Equal(1, CountOccurrences(text, "function Get-Foo"));
        Assert.DoesNotContain("theirs", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Remove_DeletesEveryMatchingAliasLine()
    {
        File.WriteAllText(_profile, string.Join(Environment.NewLine,
        [
            "# header",
            "New-Alias -Name ll -Value Get-ChildItem -Option None -Scope Local",
            "Set-Alias -Name ll -Value Get-Item -Option None -Scope Global -Force",
            "New-Alias -Name other -Value Get-Date -Option None -Scope Local",
            "Write-Host done",
        ]) + Environment.NewLine);

        var removed = new ProfileAliasWriter(_profile).Remove("ll");

        Assert.Equal(2, removed);
        var lines = File.ReadAllLines(_profile);
        Assert.Equal(["# header", "New-Alias -Name other -Value Get-Date -Option None -Scope Local", "Write-Host done"], lines);
    }

    [Fact]
    public void Remove_AlsoRemovesReferencedFunctionThatPowerPlugWrote()
    {
        var writer = new ProfileAliasWriter(_profile);
        File.WriteAllText(_profile, "function Keep-Me { 'keep' }" + Environment.NewLine);
        writer.Append("New-Alias -Name gf -Value Get-Foo -Option None -Scope Local", "Get-Foo", "if ($true) { 'nested' }");

        writer.Remove("gf");

        var text = File.ReadAllText(_profile);
        Assert.DoesNotContain("Get-Foo", text, StringComparison.Ordinal);
        Assert.Contains("function Keep-Me { 'keep' }", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Remove_LeavesHandWrittenFunctionAlone()
    {
        File.WriteAllText(_profile, string.Join(Environment.NewLine,
        [
            "function Get-Foo { 'mine, do not touch' }",
            "New-Alias -Name gf -Value Get-Foo -Option None -Scope Local",
        ]) + Environment.NewLine);

        new ProfileAliasWriter(_profile).Remove("gf");

        var text = File.ReadAllText(_profile);
        Assert.Contains("function Get-Foo { 'mine, do not touch' }", text, StringComparison.Ordinal);
        Assert.DoesNotContain("New-Alias", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Remove_KeepsFunctionStillUsedByAnotherAlias()
    {
        var writer = new ProfileAliasWriter(_profile);
        writer.Append("New-Alias -Name a -Value Get-Foo -Option None -Scope Local", "Get-Foo", "'shared'");
        writer.Append("New-Alias -Name b -Value Get-Foo -Option None -Scope Local", "Get-Foo", "'shared'");

        writer.Remove("a");

        var text = File.ReadAllText(_profile);
        Assert.Contains("function Get-Foo", text, StringComparison.Ordinal);
        Assert.Contains("-Name b", text, StringComparison.Ordinal);
        Assert.DoesNotContain("-Name a ", text, StringComparison.Ordinal);

        writer.Remove("b");
        Assert.Equal(string.Empty, File.ReadAllText(_profile));
    }

    [Fact]
    public void Remove_HandlesQuotedValues()
    {
        File.WriteAllText(_profile, "New-Alias -Name q -Value 'C:\\Program Files\\tool.exe' -Option None -Scope Local\n");
        Assert.Equal(1, new ProfileAliasWriter(_profile).Remove("q"));
        Assert.Equal(string.Empty, File.ReadAllText(_profile));
    }

    [Fact]
    public void Remove_DoesNotTouchSimilarNames()
    {
        File.WriteAllText(_profile, "New-Alias -Name ll -Value a -Option None -Scope Local\nNew-Alias -Name lls -Value b -Option None -Scope Local\n");
        Assert.Equal(1, new ProfileAliasWriter(_profile).Remove("ll"));
        Assert.Contains("-Name lls", File.ReadAllText(_profile), StringComparison.Ordinal);
    }

    [Fact]
    public void Remove_ReturnsZeroWhenNothingMatchesOrFileMissing()
    {
        Assert.Equal(0, new ProfileAliasWriter(_profile).Remove("nope"));
        File.WriteAllText(_profile, "Write-Host hi\n");
        Assert.Equal(0, new ProfileAliasWriter(_profile).Remove("nope"));
        Assert.Equal("Write-Host hi\n", File.ReadAllText(_profile));
    }

    [Fact]
    public void ContainsFunction_MatchesWholeName()
    {
        File.WriteAllText(_profile, "function Get-Foo { 1 }\n");
        var writer = new ProfileAliasWriter(_profile);
        Assert.True(writer.ContainsFunction("Get-Foo"));
        Assert.False(writer.ContainsFunction("Get-Fo"));
        Assert.False(writer.ContainsFunction("Get-FooBar"));
    }

    [Theory]
    [InlineData("New", "ll", "Get-ChildItem", "None", "Local", false, null, "New-Alias -Name ll -Value Get-ChildItem -Option None -Scope Local")]
    [InlineData("Set", "ll", "Get-ChildItem", "ReadOnly, AllScope", "Global", true, null, "Set-Alias -Name ll -Value Get-ChildItem -Option 'ReadOnly, AllScope' -Scope Global -Force")]
    [InlineData("New", "ll", "Get-ChildItem", "None", "Local", false, "list things", "New-Alias -Name ll -Value Get-ChildItem -Option None -Scope Local -Description 'list things'")]
    [InlineData("New", "np", "C:\\Program Files\\np.exe", "None", "Local", false, null, "New-Alias -Name np -Value 'C:\\Program Files\\np.exe' -Option None -Scope Local")]
    [InlineData("New", "q", "it's", "None", "Local", false, null, "New-Alias -Name q -Value 'it''s' -Option None -Scope Local")]
    public void FormatAliasCommand_ProducesValidPowerShell(string verb, string name, string value, string option, string scope, bool force, string? description, string expected)
    {
        var line = ProfileAliasWriter.FormatAliasCommand(verb, name, value, Enum.Parse<ScopedItemOptions>(option), scope, force, description);
        Assert.Equal(expected, line);
    }

    [Fact]
    public void FormatAliasCommand_RoundTripsThroughRemove()
    {
        var line = ProfileAliasWriter.FormatAliasCommand("New", "np", "C:\\Program Files\\np.exe", ScopedItemOptions.None, "Local", false, "has 'quotes'");
        var writer = new ProfileAliasWriter(_profile);
        writer.Append(line);
        Assert.Equal(1, writer.Remove("np"));
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
