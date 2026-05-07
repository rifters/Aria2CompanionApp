using Aria2Client;

namespace TrayApp;

/// <summary>
/// Watches the clipboard for magnet links and prompts the user to send them to Aria2.
/// Uses a hidden WinForms window to receive WM_CLIPBOARDUPDATE messages.
/// </summary>
internal sealed class ClipboardWatcher : IDisposable
{
    private readonly Aria2RpcClient _rpc;
    private ClipboardListenerWindow? _listenerWindow;
    private string _lastClipboard = string.Empty;
    private bool _disposed;

    public ClipboardWatcher(Aria2RpcClient rpc)
    {
        _rpc = rpc;
    }

    public void Start()
    {
        _listenerWindow = new ClipboardListenerWindow();
        _listenerWindow.ClipboardChanged += OnClipboardChanged;
    }

    public void Stop()
    {
        _listenerWindow?.Dispose();
        _listenerWindow = null;
    }

    private void OnClipboardChanged()
    {
        try
        {
            if (!Clipboard.ContainsText()) return;

            var text = Clipboard.GetText().Trim();
            if (text == _lastClipboard) return;

            _lastClipboard = text;

            if (!IsMagnetLink(text) && !IsHttpUri(text)) return;

            var label = IsMagnetLink(text) ? "magnet link" : "URL";
            var result = MessageBox.Show(
                $"Clipboard contains a {label}:\n\n{Truncate(text, 80)}\n\nSend to Aria2?",
                "Aria2 Companion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _ = SendToAria2Async(text);
            }
        }
        catch
        {
            // Clipboard can throw; ignore
        }
    }

    private async Task SendToAria2Async(string url)
    {
        try
        {
            await _rpc.AddUriAsync(url);
            Notifications.ShowInfo("Sent to Aria2", Truncate(url, 60));
        }
        catch (Exception ex)
        {
            Notifications.ShowInfo("Failed to send to Aria2", ex.Message);
        }
    }

    private static bool IsMagnetLink(string text)
        => text.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase);

    private static bool IsHttpUri(string text)
        => text.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || text.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "…";

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Hidden window that hooks into Windows clipboard change notifications.
/// </summary>
internal sealed class ClipboardListenerWindow : NativeWindow, IDisposable
{
    private const int WM_CLIPBOARDUPDATE = 0x031D;

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    public event Action? ClipboardChanged;

    public ClipboardListenerWindow()
    {
        CreateHandle(new CreateParams { Style = 0 });
        AddClipboardFormatListener(Handle);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_CLIPBOARDUPDATE)
            ClipboardChanged?.Invoke();
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        RemoveClipboardFormatListener(Handle);
        DestroyHandle();
        GC.SuppressFinalize(this);
    }
}
