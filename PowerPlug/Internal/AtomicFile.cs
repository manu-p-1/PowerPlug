namespace PowerPlug.Internal;

/// <summary>
/// Replaces a file's content by writing next to it and renaming over the original, so a crash or Ctrl+C
/// mid write leaves either the old file or the new one, never a truncated mix.
/// </summary>
internal static class AtomicFile
{
    public static void Replace(string path, byte[] content)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var temp = Path.Combine(directory, "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp");

        try
        {
            File.WriteAllBytes(temp, content);
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(temp, File.GetUnixFileMode(path));
            }

            File.Move(temp, path, overwrite: true);
        }
        catch
        {
            try
            {
                File.Delete(temp);
            }
            catch (IOException)
            {
                // The rename already consumed it, or it never got created.
            }

            throw;
        }
    }
}
