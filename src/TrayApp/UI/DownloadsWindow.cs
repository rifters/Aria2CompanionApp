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

    public DownloadsWindow(Aria2RpcClient rpc, DownloadManager manager)
    {
        _rpc = rpc;
        _manager = manager;

        InitializeComponent();
        _manager.Updated += (_, _) => SafeInvoke(RefreshDisplay);

        RefreshDisplay();
    }

    private void InitializeComponent()
    {
        Text = "Aria2 Companion – Downloads";
        Width = 820;
        Height = 560;
        MinimumSize = new Size(640, 400);
        StartPosition = FormStartPosition.CenterScreen;

        var toolbar = new ToolStrip();
        var btnAdd = new ToolStripButton("➕ Add URL") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        btnAdd.Click += (_, _) =>
        {
            using var dlg = new AddUrlDialog(_rpc);
            dlg.ShowDialog(this);
        };
        var btnRefresh = new ToolStripButton("🔄 Refresh") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        btnRefresh.Click += async (_, _) => await _manager.RefreshAsync();
        toolbar.Items.Add(btnAdd);
        toolbar.Items.Add(btnRefresh);

        _tabs = new TabControl { Dock = DockStyle.Fill };

        _activeList = CreateListView();
        _waitingList = CreateListView();
        _completedList = CreateListView();

        _tabs.TabPages.Add(new TabPage("Active") { Controls = { _activeList } });
        _tabs.TabPages.Add(new TabPage("Waiting") { Controls = { _waitingList } });
        _tabs.TabPages.Add(new TabPage("Completed") { Controls = { _completedList } });

        _statusBar = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel("Ready");
        _statusBar.Items.Add(_statusLabel);

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
            MultiSelect = false
        };
        lv.Columns.Add("Name", 300);
        lv.Columns.Add("Progress", 90);
        lv.Columns.Add("Speed", 90);
        lv.Columns.Add("ETA", 80);
        lv.Columns.Add("Size", 90);
        lv.Columns.Add("GID", 100);
        return lv;
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
}
