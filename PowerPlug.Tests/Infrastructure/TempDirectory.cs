namespace PowerPlug.Tests.Infrastructure;

/// <summary>
/// A throw away directory that is deleted when the test finishes.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("powerplug-tests-").FullName;

    /// <summary>
    /// Creates a file under the directory with the given content and returns its full path.
    /// </summary>
    public string WriteFile(string relativePath, string content)
    {
        var full = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return full;
    }

    /// <summary>
    /// Creates a file with the given bytes and returns its full path.
    /// </summary>
    public string WriteBytes(string relativePath, byte[] content)
    {
        var full = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, content);
        return full;
    }

    /// <summary>
    /// Creates an empty sub directory and returns its full path.
    /// </summary>
    public string CreateDirectory(string relativePath)
    {
        var full = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(full);
        return full;
    }

    /// <summary>
    /// Full path of a child.
    /// </summary>
    public string Combine(string relativePath) => System.IO.Path.Combine(Path, relativePath);

    public void Dispose()
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(Path, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort; the OS cleans temp eventually.
        }
    }
}
