namespace TrayApp;

/// <summary>
/// Translates Linux/NAS paths from Aria2 server to Windows paths on local machine.
/// Uses PathMappings configured in settings.json.
/// </summary>
internal static class PathMapper
{
    /// <summary>
    /// Converts a Linux path from Aria2 to a Windows path using configured mappings.
    /// Uses longest-prefix-match algorithm.
    /// </summary>
    /// <param name="linuxPath">Path from Aria2 server (e.g., /share/downloads/file.zip)</param>
    /// <returns>Translated Windows path (e.g., X:\downloads\file.zip) or original if no mapping</returns>
    public static string ToWindowsPath(string linuxPath)
    {
        if (string.IsNullOrWhiteSpace(linuxPath))
            return linuxPath;

        var mappings = SettingsManager.Instance.Settings.PathMappings;

        // Sort by prefix length descending - longest match wins
        // This allows /share/downloads to override /share/
        var sortedMappings = mappings
            .Where(m => !string.IsNullOrWhiteSpace(m.LinuxPrefix))
            .OrderByDescending(m => m.LinuxPrefix.Length);

        foreach (var mapping in sortedMappings)
        {
            // Case-insensitive prefix match
            if (linuxPath.StartsWith(mapping.LinuxPrefix, StringComparison.OrdinalIgnoreCase))
            {
                // Remove the Linux prefix
                var relativePath = linuxPath.Substring(mapping.LinuxPrefix.Length).TrimStart('/');

                // Combine with Windows prefix
                var windowsPath = Path.Combine(mapping.WindowsPrefix, relativePath);

                // Ensure proper Windows path separators
                return windowsPath.Replace('/', '\\');
            }
        }

        // No mapping found - return original path with Windows separators
        // This handles cases where:
        // 1. No mappings configured yet (first run)
        // 2. Path doesn't match any mapping
        return linuxPath.Replace('/', '\\');
    }

    /// <summary>
    /// Converts a Windows path to a Linux path (reverse mapping).
    /// Useful for displaying paths or debugging.
    /// </summary>
    /// <param name="windowsPath">Local Windows path</param>
    /// <returns>Linux path or original if no mapping found</returns>
    public static string ToLinuxPath(string windowsPath)
    {
        if (string.IsNullOrWhiteSpace(windowsPath))
            return windowsPath;

        var mappings = SettingsManager.Instance.Settings.PathMappings;

        var sortedMappings = mappings
            .Where(m => !string.IsNullOrWhiteSpace(m.WindowsPrefix))
            .OrderByDescending(m => m.WindowsPrefix.Length);

        foreach (var mapping in sortedMappings)
        {
            if (windowsPath.StartsWith(mapping.WindowsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = windowsPath.Substring(mapping.WindowsPrefix.Length).TrimStart('\\');
                var linuxPath = mapping.LinuxPrefix.TrimEnd('/') + "/" + relativePath;
                return linuxPath.Replace('\\', '/');
            }
        }

        return windowsPath.Replace('\\', '/');
    }

    /// <summary>
    /// Tests if a Linux path would successfully map to an existing Windows path.
    /// </summary>
    /// <param name="linuxPath">Path from Aria2 server</param>
    /// <returns>True if mapping exists and Windows path is accessible</returns>
    public static bool CanMapToExistingPath(string linuxPath)
    {
        var windowsPath = ToWindowsPath(linuxPath);

        // Check if mapping actually changed the path
        if (windowsPath == linuxPath || windowsPath == linuxPath.Replace('/', '\\'))
            return false;

        // Check if the mapped path exists
        return File.Exists(windowsPath) || Directory.Exists(windowsPath);
    }
}
