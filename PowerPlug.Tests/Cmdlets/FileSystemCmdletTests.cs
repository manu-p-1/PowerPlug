using System.Management.Automation;
using PowerPlug.Cmdlets.FileSystem;
using PowerPlug.Internal;
using PowerPlug.Models;
using PowerPlug.Tests.Infrastructure;

namespace PowerPlug.Tests.Cmdlets;

public class NewTemporaryDirectoryCmdletTests
{
    [Fact]
    public void CreatesDirectoryUnderTemp()
    {
        var h = new CmdletHarness<NewTemporaryDirectoryCmdlet>();
        h.Run();
        var dir = h.Only<DirectoryInfo>();
        try
        {
            Assert.True(dir.Exists);
            Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), dir.FullName, StringComparison.Ordinal);
        }
        finally
        {
            dir.Delete();
        }
    }

    [Fact]
    public void PrefixIsApplied()
    {
        var h = new CmdletHarness<NewTemporaryDirectoryCmdlet>();
        h.Cmdlet.Prefix = "pp-test-";
        h.Run();
        var dir = h.Only<DirectoryInfo>();
        try
        {
            Assert.StartsWith("pp-test-", dir.Name, StringComparison.Ordinal);
            Assert.Contains(h.Verbose, v => v.Contains(dir.FullName, StringComparison.Ordinal));
        }
        finally
        {
            dir.Delete();
        }
    }

    [Fact]
    public void PrefixWithSeparatorWritesErrorInsteadOfThrowing()
    {
        var h = new CmdletHarness<NewTemporaryDirectoryCmdlet>();
        h.Cmdlet.Prefix = "bad" + Path.DirectorySeparatorChar + "prefix";
        h.Run();
        Assert.Empty(h.Output);
        Assert.Single(h.Errors);
    }
}

public class GetDirectorySizeCmdletTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public GetDirectorySizeCmdletTests()
    {
        _temp.WriteFile("a.txt", "12345");
        _temp.WriteFile(Path.Combine("sub1", "b.txt"), "1234567890");
        _temp.WriteFile(Path.Combine("sub2", "c.txt"), "12");
        _temp.WriteFile(Path.Combine("sub2", "deep", "d.txt"), "1");
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void ReportsTotalForRoot()
    {
        var h = new CmdletHarness<GetDirectorySizeCmdlet>();
        h.Cmdlet.Path = [_temp.Path];
        h.Run();

        var info = h.Only<DirectorySizeInfo>();
        Assert.Equal(18, info.SizeBytes);
        Assert.Equal("18 B", info.Size);
        Assert.Equal(4, info.FileCount);
        Assert.Equal(3, info.DirectoryCount);
        Assert.Equal(_temp.Path, info.Path);
    }

    [Fact]
    public void ChildrenReportsOneRowPerSubdirectory()
    {
        var h = new CmdletHarness<GetDirectorySizeCmdlet>();
        h.Cmdlet.Path = [_temp.Path];
        h.Cmdlet.Children = true;
        h.Run();

        var rows = h.OutputOf<DirectorySizeInfo>();
        Assert.Equal(2, rows.Count);
        Assert.Equal(10, rows.Single(r => r.Path.EndsWith("sub1", StringComparison.Ordinal)).SizeBytes);
        Assert.Equal(3, rows.Single(r => r.Path.EndsWith("sub2", StringComparison.Ordinal)).SizeBytes);
    }

    [Fact]
    public void MissingDirectoryWritesError()
    {
        var h = new CmdletHarness<GetDirectorySizeCmdlet>();
        h.Cmdlet.Path = [_temp.Combine("nope")];
        h.Run();
        Assert.Empty(h.Output);
        Assert.Equal(ErrorCategory.ObjectNotFound, h.OnlyError("DirectoryNotFound").CategoryInfo.Category);
    }

    [Fact]
    public void MultiplePathsProduceOneRowEach()
    {
        var h = new CmdletHarness<GetDirectorySizeCmdlet>();
        h.Cmdlet.Path = [_temp.Combine("sub1"), _temp.Combine("sub2")];
        h.Run();

        var rows = h.OutputOf<DirectorySizeInfo>();
        Assert.Equal([10L, 3L], rows.Select(r => r.SizeBytes));
    }

    [Fact]
    public void ChildrenOnLeafDirectoryProducesNothing()
    {
        var h = new CmdletHarness<GetDirectorySizeCmdlet>();
        h.Cmdlet.Path = [_temp.Combine("sub1")];
        h.Cmdlet.Children = true;
        h.Run();
        Assert.Empty(h.Output);
        Assert.Empty(h.Errors);
    }

    [Fact]
    [System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
    public void UnreadableDirectoryIsCountedAsSkipped()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows() || Environment.IsPrivilegedProcess, "Needs Unix permissions and a non-root user.");
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var locked = _temp.CreateDirectory("locked");
        File.WriteAllText(Path.Combine(locked, "hidden.txt"), "secret");
        File.SetUnixFileMode(locked, UnixFileMode.None);
        try
        {
            var h = new CmdletHarness<GetDirectorySizeCmdlet>();
            h.Cmdlet.Path = [_temp.Path];
            h.Run();

            var info = h.Only<DirectorySizeInfo>();
            Assert.Equal(1, info.SkippedCount);
            Assert.Equal(18, info.SizeBytes);
            Assert.Contains(h.Verbose, v => v.Contains("Skipped unreadable folder", StringComparison.Ordinal));
        }
        finally
        {
            File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }
}

public class FindDuplicateFileCmdletTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void GroupsIdenticalContentAndOrdersByWaste()
    {
        _temp.WriteFile("a1.txt", "same content here");
        _temp.WriteFile(Path.Combine("sub", "a2.txt"), "same content here");
        _temp.WriteFile("a3.txt", "same content here");
        _temp.WriteFile("b1.txt", "other");
        _temp.WriteFile("b2.txt", "other");
        _temp.WriteFile("unique.txt", "unique");
        _temp.WriteFile("samesize.txt", "othez"); // same length as "other" but different bytes

        var h = new CmdletHarness<FindDuplicateFileCmdlet>();
        h.Cmdlet.Path = [_temp.Path];
        h.Cmdlet.Recurse = true;
        h.Run();

        var groups = h.OutputOf<DuplicateFileGroup>();
        Assert.Equal(2, groups.Count);

        var big = groups[0];
        Assert.Equal(3, big.Count);
        Assert.Equal(17, big.SizeBytes);
        Assert.Equal("17 B", big.Size);
        Assert.Equal(17 * 2, big.WastedBytes);
        Assert.Equal(Hashing.ComputeHash(System.Text.Encoding.UTF8.GetBytes("same content here"), "SHA256"), big.Hash);
        Assert.Equal(3, big.Files.Count);
        Assert.Contains(big.Files, f => f.EndsWith("a2.txt", StringComparison.Ordinal));
        Assert.Equal(big.Files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase), big.Files);

        Assert.Equal(2, groups[1].Count);
        Assert.Equal(5, groups[1].SizeBytes);
    }

    [Fact]
    public void AlgorithmParameterChangesHashLength()
    {
        _temp.WriteFile("a.txt", "same");
        _temp.WriteFile("b.txt", "same");

        var h = new CmdletHarness<FindDuplicateFileCmdlet>();
        h.Cmdlet.Path = [_temp.Path];
        h.Cmdlet.Algorithm = "MD5";
        h.Run();
        Assert.Equal(32, h.Only<DuplicateFileGroup>().Hash.Length);
    }

    [Fact]
    public void RootsAccumulateAcrossPipelineRecords()
    {
        _temp.WriteFile(Path.Combine("one", "x.txt"), "dup");
        _temp.WriteFile(Path.Combine("two", "y.txt"), "dup");

        var h = new CmdletHarness<FindDuplicateFileCmdlet>();
        h.Run(c => c.Path = [_temp.Combine("one")], c => c.Path = [_temp.Combine("two")]);

        Assert.Equal(2, h.Only<DuplicateFileGroup>().Count);
    }

    [Fact]
    public void MissingRootWritesErrorAndContinues()
    {
        _temp.WriteFile("a.txt", "same");
        _temp.WriteFile("b.txt", "same");

        var h = new CmdletHarness<FindDuplicateFileCmdlet>();
        h.Cmdlet.Path = [_temp.Combine("nope"), _temp.Path];
        h.Run();

        h.OnlyError("DirectoryNotFound");
        Assert.Single(h.OutputOf<DuplicateFileGroup>());
    }

    [Fact]
    public void WithoutRecurseIgnoresSubdirectories()
    {
        _temp.WriteFile("a1.txt", "same");
        _temp.WriteFile(Path.Combine("sub", "a2.txt"), "same");

        var h = new CmdletHarness<FindDuplicateFileCmdlet>();
        h.Cmdlet.Path = [_temp.Path];
        h.Run();
        Assert.Empty(h.Output);
    }

    [Fact]
    public void EmptyFilesAreIgnoredByDefault()
    {
        _temp.WriteFile("e1.txt", string.Empty);
        _temp.WriteFile("e2.txt", string.Empty);

        var h = new CmdletHarness<FindDuplicateFileCmdlet>();
        h.Cmdlet.Path = [_temp.Path];
        h.Run();
        Assert.Empty(h.Output);
    }

    [Fact]
    public void MinimumSizeZeroIncludesEmptyFiles()
    {
        _temp.WriteFile("e1.txt", string.Empty);
        _temp.WriteFile("e2.txt", string.Empty);

        var h = new CmdletHarness<FindDuplicateFileCmdlet>();
        h.Cmdlet.Path = [_temp.Path];
        h.Cmdlet.MinimumSize = 0;
        h.Run();
        Assert.Equal(2, h.Only<DuplicateFileGroup>().Count);
    }

    [Fact]
    public void MinimumSizeFiltersSmallFiles()
    {
        _temp.WriteFile("s1.txt", "tiny");
        _temp.WriteFile("s2.txt", "tiny");

        var h = new CmdletHarness<FindDuplicateFileCmdlet>();
        h.Cmdlet.Path = [_temp.Path];
        h.Cmdlet.MinimumSize = 100;
        h.Run();
        Assert.Empty(h.Output);
    }

    [Fact]
    public void OverlappingRootsDoNotPairAFileWithItself()
    {
        _temp.WriteFile(Path.Combine("sub", "only.txt"), "content");

        var h = new CmdletHarness<FindDuplicateFileCmdlet>();
        h.Cmdlet.Path = [_temp.Path, _temp.Combine("sub")];
        h.Cmdlet.Recurse = true;
        h.Run();
        Assert.Empty(h.Output);
    }
}

public class RemoveEmptyDirectoryCmdletTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void RemovesNestedEmptyDirectoriesBottomUp()
    {
        _temp.CreateDirectory(Path.Combine("empty", "deeper", "deepest"));
        _temp.WriteFile(Path.Combine("full", "keep.txt"), "x");
        _temp.CreateDirectory(Path.Combine("full", "emptychild"));

        var h = new CmdletHarness<RemoveEmptyDirectoryCmdlet>();
        h.Cmdlet.Path = [_temp.Path];
        h.Cmdlet.PassThru = true;
        h.Run();

        Assert.False(Directory.Exists(_temp.Combine("empty")));
        Assert.False(Directory.Exists(_temp.Combine(Path.Combine("full", "emptychild"))));
        Assert.True(Directory.Exists(_temp.Combine("full")));
        Assert.True(Directory.Exists(_temp.Path));
        Assert.Equal(4, h.OutputOf<string>().Count);
    }

    [Fact]
    public void IncludeRootRemovesRootWhenEmpty()
    {
        var root = _temp.CreateDirectory("root");
        Directory.CreateDirectory(Path.Combine(root, "a"));

        var h = new CmdletHarness<RemoveEmptyDirectoryCmdlet>();
        h.Cmdlet.Path = [root];
        h.Cmdlet.IncludeRoot = true;
        h.Run();

        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public void WhatIfLeavesEverythingInPlace()
    {
        var empty = _temp.CreateDirectory("empty");

        var h = new CmdletHarness<RemoveEmptyDirectoryCmdlet>(shouldProcess: false);
        h.Cmdlet.Path = [_temp.Path];
        h.Run();

        Assert.True(Directory.Exists(empty));
        Assert.Contains(empty, h.ShouldProcessTargets);
    }

    [Fact]
    public void WithoutPassThruNothingIsWrittenButDirectoriesGo()
    {
        var empty = _temp.CreateDirectory("empty");

        var h = new CmdletHarness<RemoveEmptyDirectoryCmdlet>();
        h.Cmdlet.Path = [_temp.Path];
        h.Run();

        Assert.Empty(h.Output);
        Assert.False(Directory.Exists(empty));
        Assert.Contains(h.Verbose, v => v.Contains("Removed 1 empty directory", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingRootWritesError()
    {
        var h = new CmdletHarness<RemoveEmptyDirectoryCmdlet>();
        h.Cmdlet.Path = [_temp.Combine("nope")];
        h.Run();
        h.OnlyError("DirectoryNotFound");
    }

    [Fact]
    public void NonEmptyTreeIsUntouched()
    {
        _temp.WriteFile(Path.Combine("a", "b", "c.txt"), "x");

        var h = new CmdletHarness<RemoveEmptyDirectoryCmdlet>();
        h.Cmdlet.Path = [_temp.Path];
        h.Cmdlet.PassThru = true;
        h.Run();

        Assert.Empty(h.Output);
        Assert.True(File.Exists(_temp.Combine(Path.Combine("a", "b", "c.txt"))));
    }
}

public class MoveTrashCmdletTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly string _trashRoot;

    public MoveTrashCmdletTests()
    {
        _trashRoot = _temp.CreateDirectory("trash-root");
        Trash.RootOverride = _trashRoot;
    }

    public void Dispose()
    {
        Trash.RootOverride = null;
        _temp.Dispose();
    }

    private static string TrashedPath(string trashRoot, string name) =>
        OperatingSystem.IsMacOS() ? Path.Combine(trashRoot, name) : Path.Combine(trashRoot, "files", name);

    [Fact]
    public void MissingPathWritesError()
    {
        var h = new CmdletHarness<MoveTrashCmdlet>();
        h.Cmdlet.Path = [_temp.Combine("missing.txt")];
        h.Run();
        Assert.Equal(ErrorCategory.ObjectNotFound, h.OnlyError("PathNotFound").CategoryInfo.Category);
    }

    [Fact]
    public void WhatIfDoesNotMoveAnything()
    {
        var file = _temp.WriteFile("keep.txt", "x");
        var h = new CmdletHarness<MoveTrashCmdlet>(shouldProcess: false);
        h.Cmdlet.Path = [file];
        h.Run();
        Assert.True(File.Exists(file));
        Assert.Equal([file], h.ShouldProcessTargets);
        Assert.Empty(Directory.EnumerateFileSystemEntries(_trashRoot));
    }

    [Fact]
    public void MovesFileAndPassThruReturnsOriginalPath()
    {
        var file = _temp.WriteFile("trash-me.txt", "payload");
        var h = new CmdletHarness<MoveTrashCmdlet>();
        h.Cmdlet.Path = [file];
        h.Cmdlet.PassThru = true;
        h.Run();

        Assert.Empty(h.Errors);
        Assert.False(File.Exists(file));
        Assert.Equal(file, h.Only<string>());
        Assert.Equal("payload", File.ReadAllText(TrashedPath(_trashRoot, "trash-me.txt")));
    }

    [Fact]
    public void MovesDirectoryWithContents()
    {
        _temp.WriteFile(Path.Combine("dir", "inner", "a.txt"), "a");
        var h = new CmdletHarness<MoveTrashCmdlet>();
        h.Cmdlet.Path = [_temp.Combine("dir")];
        h.Run();

        Assert.Empty(h.Errors);
        Assert.False(Directory.Exists(_temp.Combine("dir")));
        Assert.True(File.Exists(Path.Combine(TrashedPath(_trashRoot, "dir"), "inner", "a.txt")));
    }

    [Fact]
    public void NoPassThruProducesNoOutput()
    {
        var file = _temp.WriteFile("silent.txt", "x");
        var h = new CmdletHarness<MoveTrashCmdlet>();
        h.Cmdlet.Path = [file];
        h.Run();
        Assert.Empty(h.Output);
        Assert.Single(h.Verbose);
    }

    [Fact]
    public void MultiplePathsAreEachTrashed()
    {
        var a = _temp.WriteFile("a.log", "a");
        var b = _temp.WriteFile("b.log", "b");
        var h = new CmdletHarness<MoveTrashCmdlet>();
        h.Cmdlet.Path = [a, b];
        h.Cmdlet.PassThru = true;
        h.Run();
        Assert.Equal([a, b], h.OutputOf<string>());
        Assert.False(File.Exists(a));
        Assert.False(File.Exists(b));
    }
}

public class TrashTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void FinderLayout_NumbersCollisions()
    {
        var root = _temp.CreateDirectory("Trash");
        var first = _temp.WriteFile("a.txt", "1");
        var second = _temp.WriteFile(Path.Combine("other", "a.txt"), "2");
        var third = _temp.WriteFile(Path.Combine("third", "a.txt"), "3");

        Assert.Equal(Path.Combine(root, "a.txt"), Trash.MoveToFinderTrash(first, root));
        Assert.Equal(Path.Combine(root, "a 2.txt"), Trash.MoveToFinderTrash(second, root));
        Assert.Equal(Path.Combine(root, "a 3.txt"), Trash.MoveToFinderTrash(third, root));
        Assert.Equal("3", File.ReadAllText(Path.Combine(root, "a 3.txt")));
    }

    [Fact]
    public void XdgLayout_WritesTrashInfoBeforeMoving()
    {
        var root = _temp.Combine("xdg");
        var file = _temp.WriteFile("doc.md", "x");

        var destination = Trash.MoveToXdgTrash(file, root);

        Assert.Equal(Path.Combine(root, "files", "doc.md"), destination);
        Assert.True(File.Exists(destination));
        var info = File.ReadAllText(Path.Combine(root, "info", "doc.md.trashinfo"));
        Assert.StartsWith("[Trash Info]\n", info, StringComparison.Ordinal);
        Assert.Contains("Path=" + Uri.EscapeDataString(file).Replace("%2F", "/", StringComparison.Ordinal), info, StringComparison.Ordinal);
        Assert.Contains("DeletionDate=", info, StringComparison.Ordinal);
    }

    [Fact]
    public void XdgLayout_NumbersCollisionsInBothFolders()
    {
        var root = _temp.Combine("xdg");
        var first = _temp.WriteFile("a.txt", "1");
        var second = _temp.WriteFile(Path.Combine("other", "a.txt"), "2");

        Trash.MoveToXdgTrash(first, root);
        var destination = Trash.MoveToXdgTrash(second, root);

        Assert.Equal(Path.Combine(root, "files", "a 2.txt"), destination);
        Assert.True(File.Exists(Path.Combine(root, "info", "a 2.txt.trashinfo")));
    }

    [Fact]
    public void XdgLayout_RemovesInfoFileWhenMoveFails()
    {
        var root = _temp.Combine("xdg");
        var missing = _temp.Combine("never-existed.txt");

        Assert.ThrowsAny<IOException>(() => Trash.MoveToXdgTrash(missing, root));
        Assert.False(File.Exists(Path.Combine(root, "info", "never-existed.txt.trashinfo")));
    }
}

public class RenameBatchItemCmdletTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    private static CmdletHarness<RenameBatchItemCmdlet> Harness(string pattern, string replacement, bool shouldProcess = true)
    {
        var h = new CmdletHarness<RenameBatchItemCmdlet>(shouldProcess);
        h.Cmdlet.Pattern = pattern;
        h.Cmdlet.Replacement = replacement;
        return h;
    }

    [Fact]
    public void RenamesWithCaptureGroups()
    {
        var file = _temp.WriteFile("31-12-2023 party.jpg", "x");
        var h = Harness(@"^(\d{2})-(\d{2})-(\d{4})", "$3-$2-$1");
        h.Cmdlet.Path = [file];
        h.Run();

        var result = h.Only<RenameResult>();
        Assert.True(result.Renamed);
        Assert.Equal("2023-12-31 party.jpg", result.NewName);
        Assert.True(File.Exists(_temp.Combine("2023-12-31 party.jpg")));
    }

    [Fact]
    public void LiteralTreatsPatternAsPlainText()
    {
        var file = _temp.WriteFile("a.b.c.txt", "x");
        var h = Harness(".", "_");
        h.Cmdlet.Literal = true;
        h.Cmdlet.ExcludeExtension = true;
        h.Cmdlet.Path = [file];
        h.Run();
        Assert.Equal("a_b_c.txt", h.Only<RenameResult>().NewName);
    }

    [Fact]
    public void FirstOnlyReplacesOneMatch()
    {
        var file = _temp.WriteFile("a a a.txt", "x");
        var h = Harness(" ", "-");
        h.Cmdlet.FirstOnly = true;
        h.Cmdlet.Path = [file];
        h.Run();
        Assert.Equal("a-a a.txt", h.Only<RenameResult>().NewName);
    }

    [Theory]
    [InlineData("report.txt", "report", "summary", false, false, false, false, false, false, "summary.txt")]
    [InlineData("a.b.c.txt", ".", "_", true, false, false, true, false, false, "a_b_c.txt")]
    [InlineData("price.txt", "price", "$5", true, false, false, false, false, false, "$5.txt")]
    [InlineData("IMG_01.jpg", "img_", "pic-", false, true, false, false, false, false, "pic-01.jpg")]
    [InlineData("IMG_01.jpg", "img_", "pic-", false, false, false, false, false, false, "IMG_01.jpg")]
    [InlineData("a a a.txt", " ", "-", false, false, true, false, false, false, "a-a a.txt")]
    [InlineData("MixedCase.TXT", ".+", "$0", false, false, false, false, true, false, "mixedcase.txt")]
    [InlineData("MixedCase.TXT", ".+", "$0", false, false, false, false, false, true, "MIXEDCASE.TXT")]
    [InlineData("README", "READ", "read", false, false, false, true, false, false, "readME")]
    [InlineData("archive.tar.gz", "\\.tar$", "", false, false, false, true, false, false, "archive.gz")]
    public void ComputeNewName_AppliesEveryOption(string name, string pattern, string replacement, bool literal, bool ignoreCase, bool firstOnly, bool excludeExtension, bool toLower, bool toUpper, string expected)
    {
        var cmdlet = new RenameBatchItemCmdlet
        {
            Pattern = pattern,
            Replacement = replacement,
            Literal = literal,
            IgnoreCase = ignoreCase,
            FirstOnly = firstOnly,
            ExcludeExtension = excludeExtension,
            ToLower = toLower,
            ToUpper = toUpper,
        };

        Assert.Equal(expected, cmdlet.ComputeNewName(name, isDirectory: false));
    }

    [Fact]
    public void ComputeNewName_ExcludeExtensionIsIgnoredForDirectories()
    {
        var cmdlet = new RenameBatchItemCmdlet { Pattern = "\\.", Replacement = "_", ExcludeExtension = true };
        Assert.Equal("v1_2", cmdlet.ComputeNewName("v1.2", isDirectory: true));
        Assert.Equal("v1.2", cmdlet.ComputeNewName("v1.2", isDirectory: false));
    }

    [Theory]
    [InlineData(true, false, "mixedcase.txt")]
    [InlineData(false, true, "MIXEDCASE.TXT")]
    public void CaseTransformsRenameOnDisk(bool toLower, bool toUpper, string expected)
    {
        var file = _temp.WriteFile("MixedCase.TXT", "x");
        var h = Harness(".+", "$0");
        h.Cmdlet.ToLower = toLower;
        h.Cmdlet.ToUpper = toUpper;
        h.Cmdlet.Path = [file];
        h.Run();
        var result = h.Only<RenameResult>();
        Assert.Equal(expected, result.NewName);
        Assert.True(result.Renamed);
        Assert.Contains(Directory.EnumerateFiles(_temp.Path), f => Path.GetFileName(f) == expected);
    }

    [Fact]
    public void UnchangedNamesProduceNoOutput()
    {
        var file = _temp.WriteFile("nomatch.txt", "x");
        var h = Harness("zzz", "y");
        h.Cmdlet.Path = [file];
        h.Run();
        Assert.Empty(h.Output);
        Assert.True(File.Exists(file));
    }

    [Fact]
    public void ExistingTargetIsSkippedWithReason()
    {
        var a = _temp.WriteFile("a.txt", "a");
        _temp.WriteFile("b.txt", "b");
        var h = Harness("a", "b");
        h.Cmdlet.Path = [a];
        h.Run();

        var result = h.Only<RenameResult>();
        Assert.False(result.Renamed);
        Assert.Equal("Target already exists", result.Reason);
        Assert.Equal("a", File.ReadAllText(a));
    }

    [Fact]
    public void WhatIfReportsPreview()
    {
        var file = _temp.WriteFile("old.txt", "x");
        var h = Harness("old", "new", shouldProcess: false);
        h.Cmdlet.Path = [file];
        h.Run();

        var result = h.Only<RenameResult>();
        Assert.False(result.Renamed);
        Assert.Equal("Preview", result.Reason);
        Assert.True(File.Exists(file));
    }

    [Fact]
    public void RecurseRenamesChildrenBeforeParent()
    {
        _temp.WriteFile(Path.Combine("old dir", "old file.txt"), "x");
        var h = Harness(" ", "_");
        h.Cmdlet.Recurse = true;
        h.Cmdlet.Path = [_temp.Combine("old dir")];
        h.Run();

        Assert.Equal(2, h.OutputOf<RenameResult>().Count);
        Assert.All(h.OutputOf<RenameResult>(), r => Assert.True(r.Renamed));
        Assert.True(File.Exists(_temp.Combine(Path.Combine("old_dir", "old_file.txt"))));
    }

    [Fact]
    public void DirectoryWithoutRecurseRenamesOnlyTheDirectory()
    {
        _temp.WriteFile(Path.Combine("old dir", "old file.txt"), "x");
        var h = Harness(" ", "_");
        h.Cmdlet.Path = [_temp.Combine("old dir")];
        h.Run();

        var result = h.Only<RenameResult>();
        Assert.Equal("old_dir", result.NewName);
        Assert.True(File.Exists(_temp.Combine(Path.Combine("old_dir", "old file.txt"))));
    }

    [Fact]
    public void MissingPathWritesError()
    {
        var h = Harness("a", "b");
        h.Cmdlet.Path = [_temp.Combine("nope.txt")];
        h.Run();
        Assert.Empty(h.Output);
        h.OnlyError("PathNotFound");
    }

    [Fact]
    public void InvalidRegexTerminates()
    {
        var h = Harness("(", "x");
        h.Cmdlet.Path = [_temp.Path];
        Assert.Throws<TerminatingErrorException>(() => h.Run());
    }

    [Fact]
    public void InvalidNewNameIsSkipped()
    {
        var file = _temp.WriteFile("bad.txt", "x");
        var h = Harness("bad", OperatingSystem.IsWindows() ? "a|b" : "a/b");
        h.Cmdlet.Path = [file];
        h.Run();
        Assert.False(h.Only<RenameResult>().Renamed);
        Assert.True(File.Exists(file));
    }
}
