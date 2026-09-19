using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PowerPlug.Internal;

/// <summary>
/// Moves files and folders to the platform trash.
/// </summary>
internal static class Trash
{
    /// <summary>
    /// When set, items go to this folder instead of the platform trash. Used by tests so they never touch the
    /// real trash. On Windows the FreeDesktop layout is used under the override.
    /// </summary>
    internal static string? RootOverride { get; set; }

    /// <summary>
    /// Sends the item to the Recycle Bin (Windows), ~/.Trash (macOS) or the XDG trash (Linux).
    /// </summary>
    /// <returns>The destination path where known, otherwise a description of the destination.</returns>
    public static string MoveToTrash(string fullPath)
    {
        var root = RootOverride;
        if (root is null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            MoveToRecycleBin(fullPath);
            return "Recycle Bin";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return MoveToFinderTrash(fullPath, root ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".Trash"));
        }

        return MoveToXdgTrash(fullPath, root ?? GetXdgTrashRoot());
    }

    /// <summary>
    /// macOS layout: the item lands directly in the trash folder, with Finder style "name 2.txt" collision handling.
    /// </summary>
    internal static string MoveToFinderTrash(string fullPath, string trashRoot) => MoveInto(fullPath, trashRoot);

    [SupportedOSPlatform("windows")]
    private static void MoveToRecycleBin(string fullPath)
    {
        if (Directory.Exists(fullPath))
        {
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
                fullPath,
                Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin,
                Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);
        }
        else
        {
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                fullPath,
                Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin,
                Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);
        }
    }

    // FreeDesktop trash spec: reserve the name by creating Trash/info/<name>.trashinfo first, then move the item
    // to Trash/files/<name>. Creating the info file exclusively is what prevents two trashers from colliding.
    internal static string MoveToXdgTrash(string fullPath, string trashRoot)
    {
        var filesDir = Path.Combine(trashRoot, "files");
        var infoDir = Path.Combine(trashRoot, "info");
        Directory.CreateDirectory(filesDir);
        Directory.CreateDirectory(infoDir);

        var info = string.Create(CultureInfo.InvariantCulture,
            $"[Trash Info]\nPath={Uri.EscapeDataString(fullPath).Replace("%2F", "/", StringComparison.Ordinal)}\nDeletionDate={DateTime.Now:yyyy-MM-ddTHH:mm:ss}\n");

        var fileName = Path.GetFileName(fullPath);
        for (var attempt = 1; attempt < 10_000; attempt++)
        {
            var candidate = attempt == 1 ? fileName : NumberedName(fileName, attempt);
            var infoPath = Path.Combine(infoDir, candidate + ".trashinfo");
            var destination = Path.Combine(filesDir, candidate);

            if (Path.Exists(destination))
            {
                continue;
            }

            try
            {
                using var stream = new FileStream(infoPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                using var writer = new StreamWriter(stream);
                writer.Write(info);
            }
            catch (IOException) when (File.Exists(infoPath))
            {
                continue;
            }

            try
            {
                MoveItem(fullPath, destination);
            }
            catch
            {
                File.Delete(infoPath);
                throw;
            }

            return destination;
        }

        throw new IOException($"Could not find a free name for '{fileName}' in the trash.");
    }

    private static string GetXdgTrashRoot()
    {
        var dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (string.IsNullOrEmpty(dataHome))
        {
            dataHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        }

        return Path.Combine(dataHome, "Trash");
    }

    private static string MoveInto(string fullPath, string trashDir)
    {
        Directory.CreateDirectory(trashDir);
        var destination = UniqueDestination(trashDir, Path.GetFileName(fullPath));
        MoveItem(fullPath, destination);
        return destination;
    }

    // Rename is atomic within a volume. Across volumes (external drive, tmpfs) the OS refuses; copying the whole
    // item into the home trash could take minutes and double disk usage, so we fail with a clear message instead.
    private static void MoveItem(string source, string destination)
    {
        var isDirectory = Directory.Exists(source);
        try
        {
            if (isDirectory)
            {
                Directory.Move(source, destination);
            }
            else
            {
                File.Move(source, destination);
            }
        }
        catch (IOException ex) when (Path.Exists(source) && !Path.Exists(destination) && !SameVolume(source, destination))
        {
            throw new IOException(
                $"'{source}' is on a different volume from the trash folder and cannot be moved there. Use Remove-Item to delete it permanently.", ex);
        }
    }

    private static bool SameVolume(string a, string b)
    {
        try
        {
            var rootA = Path.GetPathRoot(Path.GetFullPath(a));
            var rootB = Path.GetPathRoot(Path.GetFullPath(b));
            if (!string.Equals(rootA, rootB, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // On Unix everything shares "/", so compare mount points through DriveInfo when available.
            return OperatingSystem.IsWindows() || MountPoint(a) == MountPoint(b);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    private static string MountPoint(string path)
    {
        var full = Path.GetFullPath(path);
        return DriveInfo.GetDrives()
            .Select(d => d.RootDirectory.FullName)
            .Where(root => full.StartsWith(root, StringComparison.Ordinal))
            .OrderByDescending(root => root.Length)
            .FirstOrDefault() ?? "/";
    }

    private static string NumberedName(string fileName, int number)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        // Dotfiles like ".env" have no stem; number them as ".env 2" rather than " 2.env".
        return stem.Length == 0
            ? string.Create(CultureInfo.InvariantCulture, $"{fileName} {number}")
            : string.Create(CultureInfo.InvariantCulture, $"{stem} {number}{extension}");
    }

    // Finder style collision handling: "name 2.txt", "name 3.txt", and so on.
    private static string UniqueDestination(string directory, string fileName)
    {
        var candidate = Path.Combine(directory, fileName);
        if (!Path.Exists(candidate))
        {
            return candidate;
        }

        for (var i = 2; i < 10_000; i++)
        {
            candidate = Path.Combine(directory, NumberedName(fileName, i));
            if (!Path.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException($"Could not find a free name for '{fileName}' in the trash.");
    }
}
