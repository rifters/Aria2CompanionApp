using Aria2Client;
using Aria2Client.Models;

namespace TrayApp.UI;

/// <summary>
/// Main downloads window showing active, waiting, and completed downloads.
/// </summary>
internal sealed class DownloadsWindow : Form
{
    private readonly Aria2RpcClient _rpc;
    private readonly DownloadManager _manager;

    private TabControl _tabs = null!;
    private ListView _activeList = null!;
    private ListView _waitingList = null!;
    private ListView _completedList = null!;
    private StatusStrip _statusBar = null!;
    private ToolStripStatusLabel _statusLabel = null!;
    private ToolStripStatusLabel _serverStatusLabel = null!;

    public DownloadsWindow(Aria2RpcClient rpc, DownloadManager manager)
    {
        _rpc = rpc;
        _manager = manager;

        InitializeComponent();
        _manager.Updated += (_, _) => SafeInvoke(RefreshDisplay);

        RefreshDisplay();
        _ = CheckServerStatusAsync();
    }

    private void InitializeComponent()
    {
        Text = "Aria2 Companion – Downloads";
        Width = 900;
        Height = 600;
        MinimumSize = new Size(700, 450);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        BackColor = SystemColors.Window;
        AutoScaleMode = AutoScaleMode.Dpi;

        var toolbar = new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            Padding = new Padding(8, 4, 8, 4),
            BackColor = Color.FromArgb(240, 240, 240),
            Font = new Font("Segoe UI", 9F)
        };

        var btnAdd = new ToolStripButton("Add URL") 
        { 
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            Image = CreateAddIcon(),
            Margin = new Padding(4, 2, 4, 2)
        };
        btnAdd.Click += (_, _) =>
        {
            using var dlg = new AddUrlDialog(_rpc);
            dlg.ShowDialog(this);
        };

        var btnRefresh = new ToolStripButton("Refresh") 
        { 
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            Image = CreateRefreshIcon(),
            Margin = new Padding(4, 2, 4, 2)
        };
        btnRefresh.Click += async (_, _) => await _manager.RefreshAsync();

        toolbar.Items.Add(btnAdd);
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(btnRefresh);

        _tabs = new TabControl 
        { 
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F),
            Padding = new Point(12, 6)
        };

        _activeList = CreateListView();
        _waitingList = CreateListView();
        _completedList = CreateListView();

        var activePage = new TabPage("Active Downloads") { Padding = new Padding(8) };
        activePage.Controls.Add(_activeList);

        var waitingPage = new TabPage("Waiting") { Padding = new Padding(8) };
        waitingPage.Controls.Add(_waitingList);

        var completedPage = new TabPage("Completed") { Padding = new Padding(8) };
        completedPage.Controls.Add(_completedList);

        _tabs.TabPages.Add(activePage);
        _tabs.TabPages.Add(waitingPage);
        _tabs.TabPages.Add(completedPage);

        _statusBar = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("Ready")
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _serverStatusLabel = new ToolStripStatusLabel("🔄 Checking server...")
        {
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleRight
        };
        _statusBar.Items.Add(_statusLabel);
        _statusBar.Items.Add(_serverStatusLabel);

        var panel = new Panel { Dock = DockStyle.Fill };
        panel.Controls.Add(_tabs);

        Controls.Add(panel);
        Controls.Add(toolbar);
        Controls.Add(_statusBar);

        // Context menus
        _activeList.ContextMenuStrip = BuildActiveContextMenu();
        _waitingList.ContextMenuStrip = BuildWaitingContextMenu();
        _completedList.ContextMenuStrip = BuildCompletedContextMenu();
    }

    // ──────────────────────────────────────────────
    //  List view builder
    // ──────────────────────────────────────────────

    private static ListView CreateListView()
    {
        var lv = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false,
            Font = new Font("Segoe UI", 9F),
            BorderStyle = BorderStyle.None
        };
        lv.Columns.Add("Name", 350);
        lv.Columns.Add("Progress", 100);
        lv.Columns.Add("Speed", 100);
        lv.Columns.Add("ETA", 90);
        lv.Columns.Add("Size", 100);
        lv.Columns.Add("GID", 120);
        return lv;
    }

    private static Bitmap CreateAddIcon()
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var pen = new Pen(Color.FromArgb(0, 120, 215), 2);
            g.DrawLine(pen, 8, 4, 8, 12);
            g.DrawLine(pen, 4, 8, 12, 8);
        }
        return bmp;
    }

    private static Bitmap CreateRefreshIcon()
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var pen = new Pen(Color.FromArgb(0, 120, 215), 2);
            g.DrawArc(pen, 3, 3, 10, 10, 0, 300);
            g.DrawLine(pen, 13, 3, 13, 6);
            g.DrawLine(pen, 13, 3, 10, 3);
        }
        return bmp;
    }

    // ──────────────────────────────────────────────
    //  Refresh display
    // ──────────────────────────────────────────────

    private void RefreshDisplay()
    {
        lock (_manager.ActiveDownloads)
            PopulateList(_activeList, _manager.ActiveDownloads);

        lock (_manager.WaitingDownloads)
            PopulateList(_waitingList, _manager.WaitingDownloads);

        lock (_manager.StoppedDownloads)
            PopulateList(_completedList, _manager.StoppedDownloads);

        if (_manager.GlobalStat is { } stat)
        {
            _statusLabel.Text =
                $"↓ {FormatSpeed(stat.DownloadSpeedBytes)}  ↑ {FormatSpeed(stat.UploadSpeedBytes)}" +
                $"  Active: {stat.NumActive}  Waiting: {stat.NumWaiting}";
        }
    }

    private static void PopulateList(ListView lv, IEnumerable<DownloadInfo> items)
    {
        lv.BeginUpdate();
        lv.Items.Clear();
        foreach (var d in items)
        {
            var item = new ListViewItem(d.Name);
            item.SubItems.Add($"{d.Progress:F1}%");
            item.SubItems.Add(FormatSpeed(d.DownloadSpeedBytes));
            item.SubItems.Add(d.Eta.HasValue ? FormatEta(d.Eta.Value) : "-");
            item.SubItems.Add(FormatSize(d.TotalLengthBytes));
            item.SubItems.Add(d.Gid);
            item.Tag = d;
            lv.Items.Add(item);
        }
        lv.EndUpdate();
    }

    // ──────────────────────────────────────────────
    //  Context menus
    // ──────────────────────────────────────────────

    private ContextMenuStrip BuildActiveContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Pause", null, async (_, _) =>
        {
            if (GetSelectedGid(_activeList) is { } gid)
                await _rpc.PauseAsync(gid);
        });
        menu.Items.Add("Remove", null, async (_, _) =>
        {
            if (GetSelectedGid(_activeList) is { } gid)
                await _rpc.ForceRemoveAsync(gid);
        });
        return menu;
    }

    private ContextMenuStrip BuildWaitingContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Resume", null, async (_, _) =>
        {
            if (GetSelectedGid(_waitingList) is { } gid)
                await _rpc.UnpauseAsync(gid);
        });
        menu.Items.Add("Remove", null, async (_, _) =>
        {
            if (GetSelectedGid(_waitingList) is { } gid)
                await _rpc.RemoveAsync(gid);
        });
        return menu;
    }

    private ContextMenuStrip BuildCompletedContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Remove from list", null, async (_, _) =>
        {
            if (GetSelectedGid(_completedList) is { } gid)
                await _rpc.RemoveAsync(gid);
        });
        menu.Items.Add("Move file…", null, (_, _) =>
        {
            if (_completedList.SelectedItems.Count == 0) return;
            if (_completedList.SelectedItems[0].Tag is DownloadInfo info)
            {
                var path = info.Files.FirstOrDefault()?.Path ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(path))
                    FileMover.ShowMoveDialog(path);
            }
        });
        return menu;
    }

    private static string? GetSelectedGid(ListView lv)
        => lv.SelectedItems.Count > 0 && lv.SelectedItems[0].Tag is DownloadInfo d ? d.Gid : null;

    // ──────────────────────────────────────────────
    //  Formatting helpers
    // ──────────────────────────────────────────────

    private static string FormatSpeed(long bytesPerSec)
    {
        if (bytesPerSec >= 1_048_576) return $"{bytesPerSec / 1_048_576.0:F1} MB/s";
        if (bytesPerSec >= 1024) return $"{bytesPerSec / 1024.0:F1} KB/s";
        return $"{bytesPerSec} B/s";
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F1} GB";
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes} B";
    }

    private static string FormatEta(TimeSpan eta)
    {
        if (eta.TotalDays >= 1) return $"{(int)eta.TotalDays}d {eta.Hours}h";
        if (eta.TotalHours >= 1) return $"{(int)eta.TotalHours}h {eta.Minutes}m";
        if (eta.TotalMinutes >= 1) return $"{(int)eta.TotalMinutes}m {eta.Seconds}s";
        return $"{eta.Seconds}s";
    }

    private void SafeInvoke(Action action)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke(action);
        else action();
    }

    private async Task CheckServerStatusAsync()
    {
        while (!IsDisposed)
        {
            try
            {
                var version = await _rpc.GetVersionAsync();
                var versionStr = version.GetProperty("version").GetString();

                SafeInvoke(() =>
                {
                    _serverStatusLabel.Text = $"✓ Aria2 v{versionStr}";
                    _serverStatusLabel.ForeColor = Color.FromArgb(0, 150, 0);
                });

                await Task.Delay(30000); // Check every 30 seconds
            }
            catch
            {
                SafeInvoke(() =>
                {
                    _serverStatusLabel.Text = "✗ Server not responding";
                    _serverStatusLabel.ForeColor = Color.FromArgb(200, 0, 0);
                });

                await Task.Delay(5000); // Retry faster when disconnected
            }
        }
    }
}
