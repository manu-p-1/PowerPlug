namespace PowerPlug.Internal;

/// <summary>
/// Walks a directory tree without following symbolic links and without stopping on folders we cannot read.
/// </summary>
internal sealed class DirectoryWalker
{
    private static readonly EnumerationOptions ChildOptions = new()
    {
        IgnoreInaccessible = false,
        RecurseSubdirectories = false,
        AttributesToSkip = FileAttributes.None,
    };

    /// <summary>Total bytes of every file visited.</summary>
    public long TotalBytes { get; private set; }

    /// <summary>Number of files visited.</summary>
    public int FileCount { get; private set; }

    /// <summary>Number of directories visited, not counting the root.</summary>
    public int DirectoryCount { get; private set; }

    /// <summary>Number of directories that could not be enumerated.</summary>
    public int SkippedCount { get; private set; }

    /// <summary>
    /// Enumerates every file under the root. Symbolic links to directories are not followed; symbolic links to
    /// files are reported but their target size is not counted.
    /// </summary>
    /// <param name="root">Directory to walk.</param>
    /// <param name="recurse">When false only the root's direct children are returned.</param>
    /// <param name="onSkipped">Called with the directory path when it could not be read.</param>
    public IEnumerable<FileInfo> EnumerateFiles(string root, bool recurse = true, Action<string>? onSkipped = null)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            FileSystemInfo[] entries;
            try
            {
                entries = new DirectoryInfo(current).GetFileSystemInfos("*", ChildOptions);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                SkippedCount++;
                onSkipped?.Invoke(current);
                continue;
            }

            foreach (var entry in entries)
            {
                if (entry is DirectoryInfo directory)
                {
                    DirectoryCount++;
                    if (recurse && directory.LinkTarget is null)
                    {
                        pending.Push(directory.FullName);
                    }

                    continue;
                }

                if (entry is FileInfo file)
                {
                    FileCount++;
                    if (file.LinkTarget is null)
                    {
                        try
                        {
                            TotalBytes += file.Length;
                        }
                        catch (FileNotFoundException)
                        {
                            // Removed between enumeration and this read.
                            FileCount--;
                            continue;
                        }
                    }

                    yield return file;
                }
            }
        }
    }

    /// <summary>
    /// Returns every directory under the root, deepest first, so callers can prune bottom up.
    /// </summary>
    public static List<string> DirectoriesDeepestFirst(string root)
    {
        var result = new List<string>();
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            result.Add(current);

            string[] children;
            try
            {
                children = Directory.GetDirectories(current);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var child in children)
            {
                if (new DirectoryInfo(child).LinkTarget is null)
                {
                    pending.Push(child);
                }
            }
        }

        result.Reverse();
        return result;
    }
}
