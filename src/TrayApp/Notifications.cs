namespace TrayApp;

/// <summary>
/// Displays Windows balloon-tip notifications from the system tray.
/// </summary>
internal static class Notifications
{
    // The NotifyIcon is borrowed from TrayIcon via a static reference set at startup.
    private static NotifyIcon? _notifyIcon;

    internal static void Register(NotifyIcon icon)
    {
        _notifyIcon = icon;
    }

    public static void ShowDownloadStarted(string gid)
    {
        Show("Download Started", $"GID: {gid}", ToolTipIcon.Info);
    }

    public static void ShowDownloadComplete(string gid)
    {
        Show("Download Complete", $"GID: {gid}", ToolTipIcon.Info);
    }

    public static void ShowDownloadError(string gid)
    {
        Show("Download Error", $"GID: {gid} encountered an error.", ToolTipIcon.Error);
    }

    public static void ShowInfo(string title, string message)
    {
        Show(title, message, ToolTipIcon.Info);
    }

    // ──────────────────────────────────────────────

    private static void Show(string title, string text, ToolTipIcon icon)
    {
        if (_notifyIcon is null) return;
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.BalloonTipIcon = icon;
        _notifyIcon.ShowBalloonTip(4000);
    }
}
