using Aria2Client;

namespace TrayApp.UI;

/// <summary>
/// Simple dialog to add a URL or magnet link to the Aria2 queue.
/// Also supports drag-and-drop of links.
/// </summary>
internal sealed class AddUrlDialog : Form
{
    private readonly Aria2RpcClient _rpc;
    private TextBox _urlBox = null!;
    private Button _btnAdd = null!;
    private Label _statusLabel = null!;

    public AddUrlDialog(Aria2RpcClient rpc)
    {
        _rpc = rpc;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Add URL / Magnet to Aria2";
        Width = 500;
        Height = 175;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        AllowDrop = true;

        var label = new Label
        {
            Text = "URL or Magnet Link:",
            AutoSize = true,
            Location = new Point(12, 18)
        };

        _urlBox = new TextBox
        {
            Location = new Point(12, 40),
            Width = 460,
            PlaceholderText = "https://… or magnet:?…"
        };

        _btnAdd = new Button
        {
            Text = "Send to Aria2",
            Location = new Point(12, 75),
            Width = 130,
            Height = 30
        };
        _btnAdd.Click += async (_, _) => await AddUrl();
        AcceptButton = _btnAdd;

        var btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(152, 75),
            Width = 80,
            Height = 30,
            DialogResult = DialogResult.Cancel
        };
        CancelButton = btnCancel;

        _statusLabel = new Label
        {
            AutoSize = true,
            Location = new Point(12, 115),
            ForeColor = Color.DarkGreen
        };

        Controls.AddRange([label, _urlBox, _btnAdd, btnCancel, _statusLabel]);

        DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.Text) == true ||
                e.Data?.GetDataPresent(DataFormats.UnicodeText) == true)
                e.Effect = DragDropEffects.Copy;
        };

        DragDrop += async (_, e) =>
        {
            var text = e.Data?.GetData(DataFormats.Text) as string
                    ?? e.Data?.GetData(DataFormats.UnicodeText) as string
                    ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
            {
                _urlBox.Text = text.Trim();
                await AddUrl();
            }
        };
    }

    private async Task AddUrl()
    {
        var url = _urlBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(url)) return;

        _btnAdd.Enabled = false;
        _statusLabel.Text = "Sending…";
        _statusLabel.ForeColor = Color.DarkBlue;

        try
        {
            var gid = await _rpc.AddUriAsync(url);
            _statusLabel.Text = $"✔ Added (GID: {gid})";
            _statusLabel.ForeColor = Color.DarkGreen;
            _urlBox.Clear();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"✘ Error: {ex.Message}";
            _statusLabel.ForeColor = Color.Red;
        }
        finally
        {
            _btnAdd.Enabled = true;
        }
    }
}
