
namespace TrayApp;

/// <summary>
/// Default configuration for Aria2 Companion App.
/// These are safe defaults for first-time users.
/// 
/// ⚠️ ACTUAL SETTINGS ARE STORED IN: settings.json (gitignored)
/// 
/// To configure:
/// 1. Run the app
/// 2. Right-click tray icon → Settings
/// 3. Enter your Aria2 server details
/// 4. Settings saved to settings.json (never committed to Git)
/// 
/// This class is kept for:
/// - Initial defaults when settings.json doesn't exist
/// - Migration from old hardcoded values (one-time)
/// - Backward compatibility
/// </summary>
internal static class Settings
{
    // Default RPC connection (localhost)
    public const string RpcUrl = "http://localhost:6800/jsonrpc";
    public const string WsUrl = "ws://localhost:6800/jsonrpc";

    // Empty by default - user must configure via Settings UI
    public const string Token = "";

    // Default download directory on Aria2 server
    public const string DefaultAria2DownloadDir = "/downloads";

    // Polling interval in milliseconds
    public const int PollIntervalMs = 3000;
}
