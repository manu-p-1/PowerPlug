namespace PowerPlug.Internal;

/// <summary>
/// Detects whether the current process runs with administrative rights.
/// </summary>
internal static class Elevation
{
    /// <summary>
    /// True for a member of the Administrators group on Windows, or effective uid 0 on Unix.
    /// </summary>
    public static bool IsElevated() => Environment.IsPrivilegedProcess;
}
