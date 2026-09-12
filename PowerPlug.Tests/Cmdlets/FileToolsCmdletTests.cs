using System.Management.Automation;
using System.Text;
using PowerPlug.Cmdlets.FileSystem;
using PowerPlug.Internal;
using PowerPlug.Models;
using PowerPlug.Tests.Infrastructure;

namespace PowerPlug.Tests.Cmdlets;

public class GetFileEncodingCmdletTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void ReportsEncodingAndLineEndings()
    {
        var utf8 = _temp.WriteFile("utf8.txt", "caf\u00e9\nline\n");
        var bom = _temp.WriteBytes("bom.txt", [0xEF, 0xBB, 0xBF, (byte)'a', (byte)'\r', (byte)'\n']);
        var binary = _temp.WriteBytes("bin.dat", [0, 1, 2, 3]);

        var h = new CmdletHarness<GetFileEncodingCmdlet>();
        h.Cmdlet.Path = [utf8, bom, binary];
        h.Run();

        var rows = h.OutputOf<FileEncodingInfo>();
        Assert.Equal(3, rows.Count);
        Assert.Equal(("UTF8", false, "LF", 2), (rows[0].Encoding, rows[0].HasBom, rows[0].LineEnding, rows[0].LineCount));
        Assert.Equal(("UTF8-BOM", true, "CRLF"), (rows[1].Encoding, rows[1].HasBom, rows[1].LineEnding));
        Assert.True(rows[2].IsBinary);
        Assert.Equal(binary, rows[2].Path);
    }

    [Fact]
    public void DirectoriesAreSkippedQuietly()
    {
        var h = new CmdletHarness<GetFileEncodingCmdlet>();
        h.Cmdlet.Path = [_temp.Path];
        h.Run();
        Assert.Empty(h.Output);
        Assert.Empty(h.Errors);
    }

    [Fact]
    public void MissingFileWritesError()
    {
        var h = new CmdletHarness<GetFileEncodingCmdlet>();
        h.Cmdlet.Path = [_temp.Combine("nope.txt")];
        h.Run();
        h.OnlyError("PathNotFound");
    }
}

public class ConvertLineEndingCmdletTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Theory]
    [InlineData("a\r\nb\r\nc", "LF", "a\nb\nc")]
    [InlineData("a\nb\nc", "CRLF", "a\r\nb\r\nc")]
    [InlineData("a\rb\nc\r\nd", "LF", "a\nb\nc\nd")]
    [InlineData("a\rb\nc\r\nd", "CRLF", "a\r\nb\r\nc\r\nd")]
    [InlineData("no endings", "LF", "no endings")]
    [InlineData("", "CRLF", "")]
    public void ConvertLineEndings_HandlesEveryStyle(string input, string to, string expected)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var inspection = TextFileInspector.Inspect(bytes);
        Assert.Equal(expected, Encoding.UTF8.GetString(TextFileInspector.ConvertLineEndings(bytes, inspection, to)));
    }

    [Fact]
    public void ConvertLineEndings_ReturnsSameArrayWhenNothingChanges()
    {
        var bytes = "a\nb\n"u8.ToArray();
        Assert.Same(bytes, TextFileInspector.ConvertLineEndings(bytes, TextFileInspector.Inspect(bytes), "LF"));
    }

    [Fact]
    public void ConvertLineEndings_DoesNotTouchNonUtf8Bytes()
    {
        // Latin-1 e-acute is 0xE9, which is not valid UTF-8 on its own. It must survive untouched.
        byte[] bytes = [(byte)'c', (byte)'a', (byte)'f', 0xE9, (byte)'\r', (byte)'\n'];
        var inspection = TextFileInspector.Inspect(bytes);
        Assert.Equal("Unknown", inspection.Encoding);
        Assert.Equal([(byte)'c', (byte)'a', (byte)'f', 0xE9, (byte)'\n'], TextFileInspector.ConvertLineEndings(bytes, inspection, "LF"));
    }

    [Fact]
    public void ConvertsCrlfToLfAndKeepsBom()
    {
        var file = _temp.WriteBytes("bom.txt", [0xEF, 0xBB, 0xBF, .. "one\r\ntwo\r\n"u8]);

        var h = new CmdletHarness<ConvertLineEndingCmdlet>();
        h.Cmdlet.Path = [file];
        h.Cmdlet.To = "LF";
        h.Run();

        var result = h.Only<LineEndingResult>();
        Assert.True(result.Changed);
        Assert.Equal("CRLF", result.From);
        Assert.Equal([0xEF, 0xBB, 0xBF, .. "one\ntwo\n"u8], File.ReadAllBytes(file));
    }

    [Fact]
    public void ConvertsUtf16WithoutMangling()
    {
        var encoding = new UnicodeEncoding(false, true);
        var file = _temp.WriteBytes("wide.txt", [.. encoding.GetPreamble(), .. encoding.GetBytes("\u00e9\nx\n")]);

        var h = new CmdletHarness<ConvertLineEndingCmdlet>();
        h.Cmdlet.Path = [file];
        h.Cmdlet.To = "CRLF";
        h.Run();

        Assert.True(h.Only<LineEndingResult>().Changed);
        Assert.Equal([.. encoding.GetPreamble(), .. encoding.GetBytes("\u00e9\r\nx\r\n")], File.ReadAllBytes(file));
    }

    [Fact]
    public void FilesAlreadyInTargetStyleAreSkippedAndOnlyReportedWithPassThru()
    {
        var file = _temp.WriteFile("lf.txt", "a\nb\n");
        var before = File.GetLastWriteTimeUtc(file);

        var quiet = new CmdletHarness<ConvertLineEndingCmdlet>();
        quiet.Cmdlet.Path = [file];
        quiet.Cmdlet.To = "LF";
        quiet.Run();
        Assert.Empty(quiet.Output);

        var verbose = new CmdletHarness<ConvertLineEndingCmdlet>();
        verbose.Cmdlet.Path = [file];
        verbose.Cmdlet.To = "LF";
        verbose.Cmdlet.PassThru = true;
        verbose.Run();
        var result = verbose.Only<LineEndingResult>();
        Assert.False(result.Changed);
        Assert.Equal("Already LF", result.Reason);
        Assert.Equal(before, File.GetLastWriteTimeUtc(file));
    }

    [Fact]
    public void BinaryFilesAreLeftAlone()
    {
        var file = _temp.WriteBytes("bin.dat", [0, 13, 10, 0]);
        var h = new CmdletHarness<ConvertLineEndingCmdlet>();
        h.Cmdlet.Path = [file];
        h.Cmdlet.To = "LF";
        h.Cmdlet.PassThru = true;
        h.Run();

        Assert.Equal("Binary file", h.Only<LineEndingResult>().Reason);
        Assert.Equal([0, 13, 10, 0], File.ReadAllBytes(file));
    }

    [Fact]
    public void WhatIfReportsPreviewWithoutWriting()
    {
        var file = _temp.WriteFile("crlf.txt", "a\r\nb\r\n");
        var h = new CmdletHarness<ConvertLineEndingCmdlet>(shouldProcess: false);
        h.Cmdlet.Path = [file];
        h.Cmdlet.To = "LF";
        h.Cmdlet.PassThru = true;
        h.Run();

        Assert.Equal("Preview", h.Only<LineEndingResult>().Reason);
        Assert.Equal("a\r\nb\r\n", File.ReadAllText(file));
    }

    [Fact]
    public void ToIsCaseInsensitive()
    {
        var file = _temp.WriteFile("crlf.txt", "a\r\nb\r\n");
        var h = new CmdletHarness<ConvertLineEndingCmdlet>();
        h.Cmdlet.Path = [file];
        h.Cmdlet.To = "lf";
        h.Run();
        Assert.Equal("LF", h.Only<LineEndingResult>().To);
        Assert.Equal("a\nb\n", File.ReadAllText(file));
    }

    [Fact]
    public void LargeFileMixedBeyondTheSampleIsStillConverted()
    {
        // LF for the first 64 KB, then CRLF. A sample based skip would wrongly report "Already LF".
        var head = new string('x', TextFileInspector.SampleSize + 100).Replace("xx", "x\n", StringComparison.Ordinal);
        var file = _temp.WriteFile("big.txt", head + "tail\r\nmore\r\n");

        var h = new CmdletHarness<ConvertLineEndingCmdlet>();
        h.Cmdlet.Path = [file];
        h.Cmdlet.To = "LF";
        h.Run();

        Assert.True(h.Only<LineEndingResult>().Changed);
        Assert.DoesNotContain("\r", File.ReadAllText(file), StringComparison.Ordinal);
    }

    [Fact]
    [System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
    public void PreservesUnixFileMode()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix permissions only.");
        if (OperatingSystem.IsWindows())
        {
            return;
        }
        var file = _temp.WriteFile("script.sh", "#!/bin/sh\r\necho hi\r\n");
        var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead;
        File.SetUnixFileMode(file, mode);

        var h = new CmdletHarness<ConvertLineEndingCmdlet>();
        h.Cmdlet.Path = [file];
        h.Cmdlet.To = "LF";
        h.Run();

        Assert.True(h.Only<LineEndingResult>().Changed);
        Assert.Equal(mode, File.GetUnixFileMode(file));
    }

    [Fact]
    public void MissingFileWritesError()
    {
        var h = new CmdletHarness<ConvertLineEndingCmdlet>();
        h.Cmdlet.Path = [_temp.Combine("nope.txt")];
        h.Cmdlet.To = "LF";
        h.Run();
        h.OnlyError("PathNotFound");
    }
}

public class SetFileTimestampCmdletTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void CreatesMissingFileAndSetsTimes()
    {
        var file = _temp.Combine("stamp");
        var when = new DateTime(2020, 5, 6, 7, 8, 9, DateTimeKind.Local);

        var h = new CmdletHarness<SetFileTimestampCmdlet>().WithParameterSet("Date");
        h.Cmdlet.Path = [file];
        h.Cmdlet.Date = when;
        h.Cmdlet.PassThru = true;
        h.Run();

        Assert.Equal(file, h.Only<FileInfo>().FullName);
        Assert.True(File.Exists(file));
        Assert.Equal(0, new FileInfo(file).Length);
        Assert.Equal(when, File.GetLastWriteTime(file));
        Assert.Equal(when, File.GetLastAccessTime(file));
    }

    [Fact]
    public void DefaultsToNowAndKeepsContent()
    {
        var file = _temp.WriteFile("existing.txt", "keep me");
        File.SetLastWriteTime(file, new DateTime(2000, 1, 1));

        var h = new CmdletHarness<SetFileTimestampCmdlet>().WithParameterSet("Date");
        h.Cmdlet.Path = [file];
        h.Run();

        Assert.Empty(h.Output);
        Assert.Equal("keep me", File.ReadAllText(file));
        Assert.True(File.GetLastWriteTime(file) > DateTime.Now.AddMinutes(-1));
    }

    [Fact]
    public void ReferenceCopiesTimesFromAnotherFile()
    {
        var reference = _temp.WriteFile("ref.txt", "r");
        var when = new DateTime(2015, 3, 4, 5, 6, 7, DateTimeKind.Local);
        File.SetLastWriteTime(reference, when);
        var target = _temp.WriteFile("target.txt", "t");

        var h = new CmdletHarness<SetFileTimestampCmdlet>().WithParameterSet("Reference");
        h.Cmdlet.Path = [target];
        h.Cmdlet.Reference = reference;
        h.Run();

        Assert.Equal(when, File.GetLastWriteTime(target));
    }

    [Fact]
    public void MissingReferenceTerminates()
    {
        var h = new CmdletHarness<SetFileTimestampCmdlet>().WithParameterSet("Reference");
        h.Cmdlet.Path = [_temp.Combine("t.txt")];
        h.Cmdlet.Reference = _temp.Combine("missing.txt");
        var ex = Assert.Throws<TerminatingErrorException>(() => h.Run());
        Assert.StartsWith("ReferenceNotFound", ex.Record.FullyQualifiedErrorId, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteTimeOnlyLeavesAccessTimeAlone()
    {
        var file = _temp.WriteFile("f.txt", "x");
        var access = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Local);
        File.SetLastAccessTime(file, access);

        var h = new CmdletHarness<SetFileTimestampCmdlet>().WithParameterSet("Date");
        h.Cmdlet.Path = [file];
        h.Cmdlet.Date = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Local);
        h.Cmdlet.WriteTimeOnly = true;
        h.Run();

        Assert.Equal(new DateTime(2021, 1, 1), File.GetLastWriteTime(file));
        Assert.Equal(access, File.GetLastAccessTime(file));
    }

    [Fact]
    public void NoCreateSkipsMissingFiles()
    {
        var file = _temp.Combine("absent.txt");
        var h = new CmdletHarness<SetFileTimestampCmdlet>().WithParameterSet("Date");
        h.Cmdlet.Path = [file];
        h.Cmdlet.NoCreate = true;
        h.Run();

        Assert.False(File.Exists(file));
        Assert.Contains(h.Verbose, v => v.StartsWith("Skipped missing file", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingParentDirectoryWritesErrorRatherThanCreatingIt()
    {
        var file = _temp.Combine(Path.Combine("no-such-dir", "stamp"));
        var h = new CmdletHarness<SetFileTimestampCmdlet>().WithParameterSet("Date");
        h.Cmdlet.Path = [file];
        h.Run();
        Assert.False(Directory.Exists(_temp.Combine("no-such-dir")));
        h.OnlyError("PathNotFound");
    }

    [Fact]
    public void DirectoriesHaveTheirTimestampUpdated()
    {
        var dir = _temp.CreateDirectory("folder");
        var when = new DateTime(2018, 2, 3, 4, 5, 6, DateTimeKind.Local);
        var h = new CmdletHarness<SetFileTimestampCmdlet>().WithParameterSet("Date");
        h.Cmdlet.Path = [dir];
        h.Cmdlet.Date = when;
        h.Cmdlet.PassThru = true;
        h.Run();
        Assert.IsType<DirectoryInfo>(Assert.Single(h.Output));
        Assert.Equal(when, Directory.GetLastWriteTime(dir));
    }

    [Fact]
    public void WhatIfCreatesNothing()
    {
        var file = _temp.Combine("preview.txt");
        var h = new CmdletHarness<SetFileTimestampCmdlet>(shouldProcess: false).WithParameterSet("Date");
        h.Cmdlet.Path = [file];
        h.Run();
        Assert.False(File.Exists(file));
        Assert.Equal([file], h.ShouldProcessTargets);
    }
}

public class GetLargestFileCmdletTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public GetLargestFileCmdletTests()
    {
        _temp.WriteFile("small.txt", "1");
        _temp.WriteFile("medium.txt", new string('m', 50));
        _temp.WriteFile("large.txt", new string('l', 500));
        _temp.WriteFile(Path.Combine("sub", "huge.txt"), new string('h', 5000));
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void ReturnsBiggestFirstAndHonoursTop()
    {
        var h = new CmdletHarness<GetLargestFileCmdlet>();
        h.Cmdlet.Path = [_temp.Path];
        h.Cmdlet.Recurse = true;
        h.Cmdlet.Top = 2;
        h.Run();

        var rows = h.OutputOf<FileSizeInfo>();
        Assert.Equal(["huge.txt", "large.txt"], rows.Select(r => r.Name));
        Assert.Equal(5000, rows[0].SizeBytes);
        Assert.Equal("4.88 KB", rows[0].Size);
        Assert.EndsWith(Path.Combine("sub", "huge.txt"), rows[0].FullName, StringComparison.Ordinal);
    }

    [Fact]
    public void WithoutRecurseStaysAtTopLevel()
    {
        var h = new CmdletHarness<GetLargestFileCmdlet>();
        h.Cmdlet.Path = [_temp.Path];
        h.Run();
        Assert.Equal(["large.txt", "medium.txt", "small.txt"], h.OutputOf<FileSizeInfo>().Select(r => r.Name));
    }

    [Fact]
    public void MinimumSizeFilters()
    {
        var h = new CmdletHarness<GetLargestFileCmdlet>();
        h.Cmdlet.Path = [_temp.Path];
        h.Cmdlet.Recurse = true;
        h.Cmdlet.MinimumSize = 100;
        h.Run();
        Assert.Equal(["huge.txt", "large.txt"], h.OutputOf<FileSizeInfo>().Select(r => r.Name));
    }

    [Fact]
    public void OverlappingRootsDoNotDuplicate()
    {
        var h = new CmdletHarness<GetLargestFileCmdlet>();
        h.Cmdlet.Path = [_temp.Path, _temp.Combine("sub")];
        h.Cmdlet.Recurse = true;
        h.Run();
        Assert.Equal(4, h.OutputOf<FileSizeInfo>().Count);
    }

    [Fact]
    public void MissingDirectoryWritesError()
    {
        var h = new CmdletHarness<GetLargestFileCmdlet>();
        h.Cmdlet.Path = [_temp.Combine("nope")];
        h.Run();
        Assert.Empty(h.Output);
        h.OnlyError("DirectoryNotFound");
    }
}

public class CompareDirectoryCmdletTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly string _left;
    private readonly string _right;

    public CompareDirectoryCmdletTests()
    {
        _left = _temp.CreateDirectory("left");
        _right = _temp.CreateDirectory("right");
        _temp.WriteFile(Path.Combine("left", "same.txt"), "same");
        _temp.WriteFile(Path.Combine("right", "same.txt"), "same");
        _temp.WriteFile(Path.Combine("left", "only-left.txt"), "l");
        _temp.WriteFile(Path.Combine("right", "only-right.txt"), "r");
        _temp.WriteFile(Path.Combine("left", "sub", "size.txt"), "short");
        _temp.WriteFile(Path.Combine("right", "sub", "size.txt"), "much longer");
        _temp.WriteFile(Path.Combine("left", "content.txt"), "aaaa");
        _temp.WriteFile(Path.Combine("right", "content.txt"), "bbbb");
    }

    public void Dispose() => _temp.Dispose();

    private CmdletHarness<CompareDirectoryCmdlet> Run(Action<CompareDirectoryCmdlet>? configure = null)
    {
        var h = new CmdletHarness<CompareDirectoryCmdlet>();
        h.Cmdlet.ReferencePath = _left;
        h.Cmdlet.DifferencePath = _right;
        h.Cmdlet.Recurse = true;
        configure?.Invoke(h.Cmdlet);
        h.Run();
        return h;
    }

    [Fact]
    public void SizeComparisonReportsOnlyAndModified()
    {
        var rows = Run().OutputOf<DirectoryComparison>();

        var byPath = rows.ToDictionary(r => r.RelativePath.Replace('\\', '/'));
        Assert.Equal("ReferenceOnly", byPath["only-left.txt"].Status);
        Assert.Equal("DifferenceOnly", byPath["only-right.txt"].Status);
        Assert.Equal(("Modified", "Size"), (byPath["sub/size.txt"].Status, byPath["sub/size.txt"].Reason));
        Assert.Equal(5, byPath["sub/size.txt"].ReferenceSize);
        Assert.Equal(11, byPath["sub/size.txt"].DifferenceSize);
        Assert.False(byPath.ContainsKey("same.txt"));
        Assert.False(byPath.ContainsKey("content.txt"), "same size, so Size mode must not flag it");
    }

    [Fact]
    public void HashComparisonCatchesSameSizeDifferentContent()
    {
        var rows = Run(c => c.CompareBy = "Hash").OutputOf<DirectoryComparison>();
        var content = Assert.Single(rows, r => r.RelativePath == "content.txt");
        Assert.Equal(("Modified", "Hash"), (content.Status, content.Reason));
    }

    [Fact]
    public void IncludeSameListsMatchingFiles()
    {
        var rows = Run(c => c.IncludeSame = true).OutputOf<DirectoryComparison>();
        Assert.Contains(rows, r => r.RelativePath == "same.txt" && r.Status == "Same");
    }

    [Fact]
    public void TimestampComparisonUsesTwoSecondTolerance()
    {
        var when = new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(Path.Combine(_left, "same.txt"), when);
        File.SetLastWriteTimeUtc(Path.Combine(_right, "same.txt"), when.AddMilliseconds(1500));
        File.SetLastWriteTimeUtc(Path.Combine(_left, "content.txt"), when);
        File.SetLastWriteTimeUtc(Path.Combine(_right, "content.txt"), when.AddSeconds(5));

        var rows = Run(c => c.CompareBy = "Timestamp").OutputOf<DirectoryComparison>();
        Assert.DoesNotContain(rows, r => r.RelativePath == "same.txt");
        var content = Assert.Single(rows, r => r.RelativePath == "content.txt");
        Assert.Equal("Timestamp", content.Reason);
    }

    [Fact]
    public void WithoutRecurseOnlyTopLevelIsCompared()
    {
        var rows = Run(c => c.Recurse = false).OutputOf<DirectoryComparison>();
        Assert.DoesNotContain(rows, r => r.RelativePath.Contains("size.txt", StringComparison.Ordinal));
    }

    [Fact]
    public void DanglingSymlinksAreIgnored()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Symlinks need elevation on Windows.");
        File.CreateSymbolicLink(Path.Combine(_left, "dangling.txt"), Path.Combine(_left, "does-not-exist.txt"));
        var rows = Run(c => c.CompareBy = "Hash").OutputOf<DirectoryComparison>();
        Assert.DoesNotContain(rows, r => r.RelativePath == "dangling.txt");
    }

    [Fact]
    public void OutputIsSortedByRelativePath()
    {
        var rows = Run(c => c.IncludeSame = true).OutputOf<DirectoryComparison>().Select(r => r.RelativePath).ToList();
        Assert.Equal(rows.OrderBy(p => p, StringComparer.OrdinalIgnoreCase), rows);
    }

    [Fact]
    public void MissingSideTerminates()
    {
        var h = new CmdletHarness<CompareDirectoryCmdlet>();
        h.Cmdlet.ReferencePath = _left;
        h.Cmdlet.DifferencePath = _temp.Combine("nope");
        var ex = Assert.Throws<TerminatingErrorException>(() => h.Run());
        Assert.StartsWith("DirectoryNotFound", ex.Record.FullyQualifiedErrorId, StringComparison.Ordinal);
    }
}

public class WaitFileCmdletTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void ReturnsImmediatelyWhenFileExists()
    {
        var file = _temp.WriteFile("here.txt", "x");
        var h = new CmdletHarness<WaitFileCmdlet>().WithParameterSet("Exists");
        h.Cmdlet.Path = file;
        h.Run();
        Assert.Equal(file, h.Only<FileInfo>().FullName);
        Assert.Empty(h.Warnings);
    }

    [Fact]
    public async Task DetectsFileCreatedLater()
    {
        var file = _temp.Combine("later.txt");
        var h = new CmdletHarness<WaitFileCmdlet>().WithParameterSet("Exists");
        h.Cmdlet.Path = file;
        h.Cmdlet.IntervalMs = 50;
        h.Cmdlet.TimeoutSeconds = 10;

        var creator = Task.Run(async () =>
        {
            await Task.Delay(300, TestContext.Current.CancellationToken);
            File.WriteAllText(file, "now");
        }, TestContext.Current.CancellationToken);

        h.Run();
        await creator;
        Assert.Equal(file, h.Only<FileInfo>().FullName);
    }

    [Fact]
    public void TimesOutWithWarningAndFalse()
    {
        var h = new CmdletHarness<WaitFileCmdlet>().WithParameterSet("Exists");
        h.Cmdlet.Path = _temp.Combine("never.txt");
        h.Cmdlet.TimeoutSeconds = 1;
        h.Cmdlet.IntervalMs = 50;
        h.Run();
        Assert.False(h.Only<bool>());
        Assert.Single(h.Warnings);
        Assert.Contains(h.Progress, p => p.RecordType == ProgressRecordType.Completed);
    }

    [Fact]
    public async Task DeletedWithoutQuietReturnsTrue()
    {
        var file = _temp.WriteFile("bye.txt", "x");
        var h = new CmdletHarness<WaitFileCmdlet>().WithParameterSet("Deleted");
        h.Cmdlet.Path = file;
        h.Cmdlet.Deleted = true;
        h.Cmdlet.IntervalMs = 50;
        h.Cmdlet.TimeoutSeconds = 10;

        var deleter = Task.Run(async () =>
        {
            await Task.Delay(200, TestContext.Current.CancellationToken);
            File.Delete(file);
        }, TestContext.Current.CancellationToken);

        h.Run();
        await deleter;
        Assert.True(h.Only<bool>());
    }

    [Fact]
    public void QuietReturnsFalseOnTimeout()
    {
        var h = new CmdletHarness<WaitFileCmdlet>().WithParameterSet("Exists");
        h.Cmdlet.Path = _temp.Combine("never.txt");
        h.Cmdlet.TimeoutSeconds = 1;
        h.Cmdlet.IntervalMs = 50;
        h.Cmdlet.Quiet = true;
        h.Run();
        Assert.False(h.Only<bool>());
    }

    [Fact]
    public async Task ChangedWaitsForContentToChange()
    {
        var file = _temp.WriteFile("log.txt", "start");
        var h = new CmdletHarness<WaitFileCmdlet>().WithParameterSet("Changed");
        h.Cmdlet.Path = file;
        h.Cmdlet.Changed = true;
        h.Cmdlet.IntervalMs = 50;
        h.Cmdlet.TimeoutSeconds = 10;
        h.Cmdlet.Quiet = true;

        var writer = Task.Run(async () =>
        {
            await Task.Delay(300, TestContext.Current.CancellationToken);
            File.AppendAllText(file, " more");
        }, TestContext.Current.CancellationToken);

        h.Run();
        await writer;
        Assert.True(h.Only<bool>());
    }

    [Fact]
    public void ChangedOnMissingFileWarnsAndTreatsCreationAsChange()
    {
        var file = _temp.Combine("not-yet.txt");
        var h = new CmdletHarness<WaitFileCmdlet>().WithParameterSet("Changed");
        h.Cmdlet.Path = file;
        h.Cmdlet.Changed = true;
        h.Cmdlet.TimeoutSeconds = 1;
        h.Cmdlet.IntervalMs = 50;
        h.Cmdlet.Quiet = true;
        h.Run();
        Assert.Contains(h.Warnings, w => w.Contains("does not exist yet", StringComparison.Ordinal));
        Assert.False(h.Only<bool>());
    }

    [Fact]
    public async Task DeletedWaitsForRemoval()
    {
        var file = _temp.WriteFile("gone.txt", "x");
        var h = new CmdletHarness<WaitFileCmdlet>().WithParameterSet("Deleted");
        h.Cmdlet.Path = file;
        h.Cmdlet.Deleted = true;
        h.Cmdlet.IntervalMs = 50;
        h.Cmdlet.TimeoutSeconds = 10;
        h.Cmdlet.Quiet = true;

        var deleter = Task.Run(async () =>
        {
            await Task.Delay(300, TestContext.Current.CancellationToken);
            File.Delete(file);
        }, TestContext.Current.CancellationToken);

        h.Run();
        await deleter;
        Assert.True(h.Only<bool>());
    }
}
