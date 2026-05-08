namespace TrayApp.Models;

/// <summary>
/// Root settings object serialized to settings.json
/// </summary>
public class AppSettings
{
    public RpcSettings RpcSettings { get; set; } = new();
    public List<PathMapping> PathMappings { get; set; } = new();
    public List<MovePreset> MovePresets { get; set; } = new();
    public int PollingIntervalMs { get; set; } = 3000;
    public bool EnableClipboardMonitoring { get; set; } = true;
    public bool ShowPathMappings { get; set; } = true;
    public string Version { get; set; } = "1.0";
}

/// <summary>
/// Aria2 RPC connection settings
/// </summary>
public class RpcSettings
{
    public string RpcUrl { get; set; } = "http://localhost:6800/jsonrpc";
    public string WsUrl { get; set; } = "ws://localhost:6800/jsonrpc";
    public string Token { get; set; } = "";
    public string DefaultDownloadDir { get; set; } = "/downloads";
}

/// <summary>
/// Maps a Linux path from Aria2 server to a Windows path on the local machine.
/// Used when Aria2 runs on a NAS or remote Linux server.
/// </summary>
public class PathMapping
{
    public string LinuxPrefix { get; set; } = "";
    public string WindowsPrefix { get; set; } = "";
    public string? Description { get; set; }
}

/// <summary>
/// Move dialog preset button configuration
/// </summary>
public class MovePreset
{
    public string Label { get; set; } = "";
    public string Path { get; set; } = "";
}
