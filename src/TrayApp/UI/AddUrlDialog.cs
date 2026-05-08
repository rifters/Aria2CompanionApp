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
        SuspendLayout();

        Text = "Add Download to Aria2";
        ClientSize = new Size(500, 220);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        AllowDrop = true;
        Font = new Font("Segoe UI", 9F);
        AutoScaleMode = AutoScaleMode.Font;
        Padding = new Padding(12);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var label = new Label
        {
            Text = "Enter a URL or Magnet Link:",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
            TabIndex = 0
        };

        _urlBox = new TextBox
        {
            Width = 476,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 0, 0, 12),
            TabIndex = 1,
            PlaceholderText = "https://example.com/file.zip or magnet:?xt=..."
        };

        var buttonPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 0, 0, 12),
            WrapContents = false
        };

        _btnAdd = new Button
        {
            Text = "Add to Aria2",
            AutoSize = true,
            MinimumSize = new Size(100, 0),
            Margin = new Padding(0, 0, 8, 0),
            TabIndex = 2
        };
        _btnAdd.Click += async (_, _) => await AddUrl();
        AcceptButton = _btnAdd;

        var btnCancel = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            MinimumSize = new Size(80, 0),
            Margin = new Padding(0),
            TabIndex = 3,
            DialogResult = DialogResult.Cancel
        };
        CancelButton = btnCancel;

        buttonPanel.Controls.Add(_btnAdd);
        buttonPanel.Controls.Add(btnCancel);

        _statusLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(476, 0),
            Margin = new Padding(0, 0, 0, 12),
            TabIndex = 4,
            ForeColor = Color.Green,
            Text = ""
        };

        var tipLabel = new Label
        {
            Text = "💡 Tip: Drag and drop URLs directly into this window",
            AutoSize = true,
            Margin = new Padding(0),
            TabIndex = 5,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8F)
        };

        mainLayout.Controls.Add(label, 0, 0);
        mainLayout.Controls.Add(_urlBox, 0, 1);
        mainLayout.Controls.Add(buttonPanel, 0, 2);
        mainLayout.Controls.Add(_statusLabel, 0, 3);
        mainLayout.Controls.Add(tipLabel, 0, 4);

        Controls.Add(mainLayout);

        ResumeLayout(false);
        PerformLayout();

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
        _statusLabel.Text = "⏳ Sending to Aria2...";
        _statusLabel.ForeColor = Color.FromArgb(100, 100, 100);

        try
        {
            // Pass download directory option from settings
            var options = new Dictionary<string, object>
            {
                { "dir", SettingsManager.Instance.Settings.RpcSettings.DefaultDownloadDir }
            };
            var gid = await _rpc.AddUriAsync(url, options);

            // Try to get filename for better UX
            var filename = url;
            try
            {
                var uri = new Uri(url);
                filename = Path.GetFileName(uri.LocalPath);
                if (string.IsNullOrWhiteSpace(filename) || filename == "/")
                {
                    filename = uri.Host;
                }
            }
            catch
            {
                // If URL parsing fails, just show truncated URL
                if (url.Length > 40)
                    filename = url.Substring(0, 37) + "...";
            }

            _statusLabel.Text = $"✓ Added: {filename}";
            _statusLabel.ForeColor = Color.FromArgb(0, 150, 0);
            _urlBox.Clear();

            // Auto-close after 1.5 seconds on success
            await Task.Delay(1500);
            if (!IsDisposed)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }
        catch (Aria2RpcException ex)
        {
            _statusLabel.Text = $"✗ Error {ex.Code}: {ex.Message}";
            _statusLabel.ForeColor = Color.FromArgb(200, 0, 0);

            MessageBox.Show(this,
                $"Aria2 could not add the download.\n\n" +
                $"Error Code: {ex.Code}\n" +
                $"Message: {ex.Message}\n\n" +
                $"URL: {url}\n\n" +
                $"Common causes:\n" +
                $"• Aria2 doesn't have write permission to download directory\n" +
                $"• Invalid URL or unsupported protocol\n" +
                $"• Network connectivity issues\n" +
                $"• Aria2 configuration issues",
                "Aria2 Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (HttpRequestException ex)
        {
            _statusLabel.Text = $"✗ Connection failed";
            _statusLabel.ForeColor = Color.FromArgb(200, 0, 0);

            MessageBox.Show(this,
                $"Cannot connect to Aria2 server.\n\n" +
                $"Error: {ex.Message}\n\n" +
                $"RPC URL: {Settings.RpcUrl}\n\n" +
                $"Please verify:\n" +
                $"• Aria2 is running\n" +
                $"• RPC server is enabled (--enable-rpc)\n" +
                $"• The IP address and port are correct\n" +
                $"• Firewall is not blocking the connection",
                "Connection Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"✗ Unexpected error";
            _statusLabel.ForeColor = Color.FromArgb(200, 0, 0);

            MessageBox.Show(this,
                $"An unexpected error occurred.\n\n" +
                $"Error: {ex.Message}\n\n" +
                $"Stack trace:\n{ex.StackTrace}",
                "Unexpected Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _btnAdd.Enabled = true;
        }
    }
}
