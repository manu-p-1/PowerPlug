namespace PowerPlug.Models;

/// <summary>
/// Text encoding and line ending details for a file.
/// </summary>
public sealed class FileEncodingInfo
{
    /// <summary>Full path of the file.</summary>
    public required string Path { get; init; }

    /// <summary>Encoding name: UTF8, UTF8-BOM, UTF16LE, UTF16BE, UTF32LE, UTF32BE, ASCII, Binary or Unknown.</summary>
    public required string Encoding { get; init; }

    /// <summary>True when the file starts with a byte order mark.</summary>
    public required bool HasBom { get; init; }

    /// <summary>LF, CRLF, CR, Mixed or None.</summary>
    public required string LineEnding { get; init; }

    /// <summary>True when the content looks like binary data rather than text.</summary>
    public required bool IsBinary { get; init; }

    /// <summary>Number of line endings found in the first 64 KB of the file.</summary>
    public required int LineCount { get; init; }
}

/// <summary>
/// Outcome of converting a file's line endings.
/// </summary>
public sealed class LineEndingResult
{
    /// <summary>Full path of the file.</summary>
    public required string Path { get; init; }

    /// <summary>Line ending style before conversion.</summary>
    public required string From { get; init; }

    /// <summary>Line ending style after conversion.</summary>
    public required string To { get; init; }

    /// <summary>True when the file content was rewritten.</summary>
    public required bool Changed { get; init; }

    /// <summary>Why the file was left alone, when it was.</summary>
    public string? Reason { get; init; }
}

/// <summary>
/// A file and its size, as reported by Get-LargestFile.
/// </summary>
public sealed class FileSizeInfo
{
    /// <summary>Full path of the file. Named so the object can be piped into cmdlets that bind FullName.</summary>
    public required string FullName { get; init; }

    /// <summary>File name without the directory.</summary>
    public required string Name { get; init; }

    /// <summary>Size in bytes.</summary>
    public required long SizeBytes { get; init; }

    /// <summary>Size formatted for reading.</summary>
    public required string Size { get; init; }

    /// <summary>Last write time.</summary>
    public required DateTime LastWriteTime { get; init; }
}

/// <summary>
/// One entry from comparing two directory trees.
/// </summary>
public sealed class DirectoryComparison
{
    /// <summary>Path relative to the roots being compared.</summary>
    public required string RelativePath { get; init; }

    /// <summary>ReferenceOnly, DifferenceOnly, Modified or Same.</summary>
    public required string Status { get; init; }

    /// <summary>Size in the reference tree, or null when absent.</summary>
    public long? ReferenceSize { get; init; }

    /// <summary>Size in the difference tree, or null when absent.</summary>
    public long? DifferenceSize { get; init; }

    /// <summary>Last write time in the reference tree, or null when absent.</summary>
    public DateTime? ReferenceModified { get; init; }

    /// <summary>Last write time in the difference tree, or null when absent.</summary>
    public DateTime? DifferenceModified { get; init; }

    /// <summary>What differed for Modified entries: Size, Hash, Timestamp, or Unreadable when a file could not be read.</summary>
    public string? Reason { get; init; }
}
