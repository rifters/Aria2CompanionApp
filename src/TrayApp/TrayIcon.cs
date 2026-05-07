using Aria2Client;
using TrayApp.UI;

namespace TrayApp;

/// <summary>
/// Hosts the Windows system-tray icon and context menu.
/// Runs as an ApplicationContext so it controls the message loop lifetime.
/// </summary>
internal sealed class TrayIcon : ApplicationContext, IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Aria2RpcClient _rpc;
    private readonly Aria2WebSocketClient _ws;
    private readonly DownloadManager _downloadManager;
    private readonly ClipboardWatcher _clipboardWatcher;
    private DownloadsWindow? _downloadsWindow;

    public TrayIcon(Aria2RpcClient rpc, Aria2WebSocketClient ws, DownloadManager downloadManager)
    {
        _rpc = rpc;
        _ws = ws;
        _downloadManager = downloadManager;
        _clipboardWatcher = new ClipboardWatcher(rpc);

        _notifyIcon = new NotifyIcon
        {
            Icon = CreateIcon(),
            Text = "Aria2 Companion",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };

        _notifyIcon.DoubleClick += (_, _) => OpenDownloadsWindow();

        Notifications.Register(_notifyIcon);
        WireWebSocketEvents();
        _ws.StartBackground();
        _downloadManager.StartPolling();
        _clipboardWatcher.Start();
    }

    // ──────────────────────────────────────────────
    //  Context menu
    // ──────────────────────────────────────────────

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var openItem = new ToolStripMenuItem("Open Downloads", null, (_, _) => OpenDownloadsWindow());
        var addItem = new ToolStripMenuItem("Add URL / Magnet", null, (_, _) => ShowAddUrlDialog());
        var settingsItem = new ToolStripMenuItem("Settings") { Enabled = false };
        var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitApp());

        menu.Items.Add(openItem);
        menu.Items.Add(addItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        return menu;
    }

    // ──────────────────────────────────────────────
    //  Actions
    // ──────────────────────────────────────────────

    private void OpenDownloadsWindow()
    {
        if (_downloadsWindow is null || _downloadsWindow.IsDisposed)
        {
            _downloadsWindow = new DownloadsWindow(_rpc, _downloadManager);
            _downloadsWindow.FormClosed += (_, _) => _downloadsWindow = null;
        }

        _downloadsWindow.Show();
        _downloadsWindow.BringToFront();
    }

    private void ShowAddUrlDialog()
    {
        using var dlg = new AddUrlDialog(_rpc);
        dlg.ShowDialog();
    }

    private void ExitApp()
    {
        _notifyIcon.Visible = false;
        _ws.Stop();
        _downloadManager.StopPolling();
        _clipboardWatcher.Stop();
        Application.Exit();
    }

    // ──────────────────────────────────────────────
    //  WebSocket events → UI thread
    // ──────────────────────────────────────────────

    private void WireWebSocketEvents()
    {
        _ws.DownloadStarted += (_, gid) => InvokeOnUiThread(() =>
        {
            Notifications.ShowDownloadStarted(gid);
            _notifyIcon.Text = $"Aria2 – Downloading…";
        });

        _ws.DownloadComplete += (_, gid) => InvokeOnUiThread(async () =>
        {
            Notifications.ShowDownloadComplete(gid);
            _notifyIcon.Text = "Aria2 Companion";

            // Fetch file info then offer to move/extract
            try
            {
                var info = await _rpc.TellStatusAsync(gid);
                var filePath = info.Files.FirstOrDefault()?.Path ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    Extractor.TryExtract(filePath);
                    FileMover.ShowMoveDialog(filePath);
                }
            }
            catch { /* best-effort */ }
        });

        _ws.DownloadError += (_, gid) => InvokeOnUiThread(() =>
        {
            Notifications.ShowDownloadError(gid);
            _notifyIcon.Text = "Aria2 Companion";
        });

        _ws.Connected += (_, _) => InvokeOnUiThread(() =>
        {
            _notifyIcon.Text = "Aria2 Companion (connected)";
        });

        _ws.Disconnected += (_, _) => InvokeOnUiThread(() =>
        {
            _notifyIcon.Text = "Aria2 Companion (reconnecting…)";
        });
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private static void InvokeOnUiThread(Action action)
    {
        if (Application.OpenForms.Count > 0)
            Application.OpenForms[0]?.BeginInvoke(action);
        else
            action();
    }

    private static void InvokeOnUiThread(Func<Task> asyncAction)
    {
        if (Application.OpenForms.Count > 0)
            Application.OpenForms[0]?.BeginInvoke(() => _ = asyncAction());
        else
            _ = asyncAction();
    }

    private static Icon CreateIcon()
    {
        // Create a simple 16×16 icon programmatically (green arrow)
        var bitmap = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            using var brush = new SolidBrush(Color.LimeGreen);
            // Draw a downward arrow shape
            g.FillPolygon(brush, new[]
            {
                new Point(8, 14), new Point(1, 5), new Point(5, 5),
                new Point(5, 1), new Point(11, 1), new Point(11, 5),
                new Point(15, 5)
            });
        }
        return Icon.FromHandle(bitmap.GetHicon());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.Dispose();
            _ws.Dispose();
            _clipboardWatcher.Dispose();
        }
        base.Dispose(disposing);
    }
}
