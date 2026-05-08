using System.ComponentModel;
using Aria2Client;
using TrayApp.Models;

namespace TrayApp.UI;

/// <summary>
/// Settings window for configuring Aria2 RPC connection and path mappings.
/// </summary>
internal sealed class SettingsWindow : Form
{
    private readonly AppSettings _workingSettings;
    private readonly BindingList<PathMapping> _mappingsBindingList;

    // RPC Settings controls
    private TextBox _txtRpcUrl = null!;
    private TextBox _txtWsUrl = null!;
    private TextBox _txtToken = null!;
    private TextBox _txtDownloadDir = null!;
    private NumericUpDown _numPollInterval = null!;

    // Path Mappings controls
    private DataGridView _gridMappings = null!;
    private Button _btnAddMapping = null!;
    private Button _btnEditMapping = null!;
    private Button _btnRemoveMapping = null!;
    private Button _btnTestMapping = null!;

    // Action buttons
    private Button _btnTestConnection = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;

    // Status
    private Label _lblStatus = null!;

    public SettingsWindow()
    {
        // Work on a copy - only save on explicit Save button click
        _workingSettings = CloneSettings(SettingsManager.Instance.Settings);

        // Use BindingList for proper DataGridView support
        _mappingsBindingList = new BindingList<PathMapping>(_workingSettings.PathMappings);

        InitializeComponent();
        LoadSettingsIntoControls();
    }

    private void InitializeComponent()
    {
        Text = "Aria2 Companion - Settings";
        Width = 800;
        Height = 650;
        MinimumSize = new Size(700, 550);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        Font = new Font("Segoe UI", 9F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoScaleMode = AutoScaleMode.Font;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // RPC group
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Mappings group
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Status
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons

        // === RPC Settings Group ===
        var rpcGroup = CreateRpcSettingsGroup();
        mainLayout.Controls.Add(rpcGroup, 0, 0);

        // === Path Mappings Group ===
        var mappingsGroup = CreatePathMappingsGroup();
        mainLayout.Controls.Add(mappingsGroup, 0, 1);

        // === Status Label ===
        _lblStatus = new Label
        {
            Text = "Ready",
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 8),
            ForeColor = Color.Gray
        };
        mainLayout.Controls.Add(_lblStatus, 0, 2);

        // === Action Buttons ===
        var buttonPanel = CreateButtonPanel();
        mainLayout.Controls.Add(buttonPanel, 0, 3);

        Controls.Add(mainLayout);
    }

    private GroupBox CreateRpcSettingsGroup()
    {
        var group = new GroupBox
        {
            Text = "Aria2 RPC Connection",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(8),
            Dock = DockStyle.Fill
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            AutoSize = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // RPC URL
        layout.Controls.Add(new Label { Text = "RPC URL:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 3, 3) }, 0, 0);
        _txtRpcUrl = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "http://localhost:6800/jsonrpc" };
        layout.Controls.Add(_txtRpcUrl, 1, 0);

        // WebSocket URL
        layout.Controls.Add(new Label { Text = "WebSocket URL:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 3, 3) }, 0, 1);
        _txtWsUrl = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "ws://localhost:6800/jsonrpc" };
        layout.Controls.Add(_txtWsUrl, 1, 1);

        // Token
        layout.Controls.Add(new Label { Text = "RPC Token:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 3, 3) }, 0, 2);
        _txtToken = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true, PlaceholderText = "Leave empty if no token configured" };
        layout.Controls.Add(_txtToken, 1, 2);

        // Download Directory
        layout.Controls.Add(new Label { Text = "Download Dir:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 3, 3) }, 0, 3);
        _txtDownloadDir = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "/downloads" };
        layout.Controls.Add(_txtDownloadDir, 1, 3);

        // Poll Interval
        layout.Controls.Add(new Label { Text = "Poll Interval (ms):", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 3, 3) }, 0, 4);
        _numPollInterval = new NumericUpDown { Minimum = 1000, Maximum = 60000, Increment = 1000, Value = 3000, Width = 100 };
        layout.Controls.Add(_numPollInterval, 1, 4);

        group.Controls.Add(layout);
        return group;
    }

    private GroupBox CreatePathMappingsGroup()
    {
        var group = new GroupBox
        {
            Text = "Path Mappings (for NAS/Remote Aria2)",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // DataGridView for mappings
        _gridMappings = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false
        };

        _gridMappings.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "LinuxPrefix",
            HeaderText = "Linux Path (Aria2 Server)",
            DataPropertyName = "LinuxPrefix",
            FillWeight = 40
        });

        _gridMappings.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "WindowsPrefix",
            HeaderText = "Windows Path (Local)",
            DataPropertyName = "WindowsPrefix",
            FillWeight = 40
        });

        _gridMappings.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Description",
            HeaderText = "Description",
            DataPropertyName = "Description",
            FillWeight = 20
        });

        layout.Controls.Add(_gridMappings, 0, 0);

        // Button panel
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        };

        _btnAddMapping = new Button { Text = "Add Mapping", AutoSize = true, Margin = new Padding(0, 0, 8, 0) };
        _btnAddMapping.Click += BtnAddMapping_Click;

        _btnEditMapping = new Button { Text = "Edit", AutoSize = true, Margin = new Padding(0, 0, 8, 0) };
        _btnEditMapping.Click += BtnEditMapping_Click;

        _btnRemoveMapping = new Button { Text = "Remove", AutoSize = true, Margin = new Padding(0, 0, 8, 0) };
        _btnRemoveMapping.Click += BtnRemoveMapping_Click;

        _btnTestMapping = new Button { Text = "Test Mapping", AutoSize = true };
        _btnTestMapping.Click += BtnTestMapping_Click;

        btnPanel.Controls.AddRange(new Control[] { _btnAddMapping, _btnEditMapping, _btnRemoveMapping, _btnTestMapping });
        layout.Controls.Add(btnPanel, 0, 1);

        group.Controls.Add(layout);
        return group;
    }

    private FlowLayoutPanel CreateButtonPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };

        _btnCancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        _btnSave = new Button { Text = "Save", AutoSize = true, Margin = new Padding(8, 0, 0, 0) };
        _btnSave.Click += BtnSave_Click;

        _btnTestConnection = new Button { Text = "Test Connection", AutoSize = true, Margin = new Padding(8, 0, 0, 0) };
        _btnTestConnection.Click += BtnTestConnection_Click;

        panel.Controls.AddRange(new Control[] { _btnCancel, _btnSave, _btnTestConnection });

        CancelButton = _btnCancel;
        AcceptButton = _btnSave;

        return panel;
    }

    private void LoadSettingsIntoControls()
    {
        _txtRpcUrl.Text = _workingSettings.RpcSettings.RpcUrl;
        _txtWsUrl.Text = _workingSettings.RpcSettings.WsUrl;
        _txtToken.Text = _workingSettings.RpcSettings.Token;
        _txtDownloadDir.Text = _workingSettings.RpcSettings.DefaultDownloadDir;
        _numPollInterval.Value = _workingSettings.PollingIntervalMs;

        RefreshMappingsGrid();
    }

    private void RefreshMappingsGrid()
    {
        _gridMappings.DataSource = null;
        _gridMappings.DataSource = _mappingsBindingList;
    }

    private void BtnAddMapping_Click(object? sender, EventArgs e)
    {
        using var dlg = new PathMappingDialog(null);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Mapping != null)
        {
            _mappingsBindingList.Add(dlg.Mapping);
            _lblStatus.Text = "Mapping added (not saved yet)";
            _lblStatus.ForeColor = Color.Orange;
        }
    }

    private void BtnEditMapping_Click(object? sender, EventArgs e)
    {
        if (_gridMappings.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select a mapping to edit.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedMapping = _gridMappings.SelectedRows[0].DataBoundItem as PathMapping;
        if (selectedMapping == null) return;

        using var dlg = new PathMappingDialog(selectedMapping);
        if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Mapping != null)
        {
            var index = _mappingsBindingList.IndexOf(selectedMapping);
            if (index >= 0)
            {
                _mappingsBindingList[index] = dlg.Mapping;
            }
            _lblStatus.Text = "Mapping updated (not saved yet)";
            _lblStatus.ForeColor = Color.Orange;
        }
    }

    private void BtnRemoveMapping_Click(object? sender, EventArgs e)
    {
        if (_gridMappings.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select a mapping to remove.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedMapping = _gridMappings.SelectedRows[0].DataBoundItem as PathMapping;
        if (selectedMapping == null) return;

        var result = MessageBox.Show(
            $"Remove mapping?\n\nLinux: {selectedMapping.LinuxPrefix}\nWindows: {selectedMapping.WindowsPrefix}",
            "Confirm Remove",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (result == DialogResult.Yes)
        {
            _mappingsBindingList.Remove(selectedMapping);
            _lblStatus.Text = "Mapping removed (not saved yet)";
            _lblStatus.ForeColor = Color.Orange;
        }
    }

    private void BtnTestMapping_Click(object? sender, EventArgs e)
    {
        if (_gridMappings.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select a mapping to test.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedMapping = _gridMappings.SelectedRows[0].DataBoundItem as PathMapping;
        if (selectedMapping == null) return;

        var windowsPath = selectedMapping.WindowsPrefix;
        var exists = Directory.Exists(windowsPath) || File.Exists(windowsPath);

        if (exists)
        {
            MessageBox.Show(
                $"? Mapping test passed!\n\n" +
                $"Linux path: {selectedMapping.LinuxPrefix}\n" +
                $"Windows path: {windowsPath}\n\n" +
                $"The Windows path exists and is accessible.",
                "Test Successful",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        else
        {
            MessageBox.Show(
                $"? Mapping test failed!\n\n" +
                $"Linux path: {selectedMapping.LinuxPrefix}\n" +
                $"Windows path: {windowsPath}\n\n" +
                $"The Windows path does not exist or is not accessible.\n" +
                $"Make sure the network share is mounted or the path is correct.",
                "Test Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }
    }

    private async void BtnTestConnection_Click(object? sender, EventArgs e)
    {
        _btnTestConnection.Enabled = false;
        _lblStatus.Text = "Testing connection...";
        _lblStatus.ForeColor = Color.Blue;

        try
        {
            var testClient = new Aria2RpcClient(_txtRpcUrl.Text.Trim(), _txtToken.Text.Trim());
            var version = await testClient.GetVersionAsync();
            var versionStr = version.GetProperty("version").GetString();

            _lblStatus.Text = $"? Connected successfully! Aria2 version: {versionStr}";
            _lblStatus.ForeColor = Color.Green;

            MessageBox.Show(
                $"? Connection successful!\n\nAria2 version: {versionStr}",
                "Test Successful",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "? Connection failed";
            _lblStatus.ForeColor = Color.Red;

            MessageBox.Show(
                $"? Connection failed!\n\n" +
                $"Error: {ex.Message}\n\n" +
                $"Please check:\n" +
                $"• Aria2 is running\n" +
                $"• RPC URL is correct\n" +
                $"• RPC secret/token matches\n" +
                $"• Firewall is not blocking the connection",
                "Connection Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        finally
        {
            _btnTestConnection.Enabled = true;
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        // Validate RPC URL
        if (string.IsNullOrWhiteSpace(_txtRpcUrl.Text))
        {
            MessageBox.Show("RPC URL is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtRpcUrl.Focus();
            return;
        }

        // Save from controls to working settings
        _workingSettings.RpcSettings.RpcUrl = _txtRpcUrl.Text.Trim();
        _workingSettings.RpcSettings.WsUrl = _txtWsUrl.Text.Trim();
        _workingSettings.RpcSettings.Token = _txtToken.Text.Trim();
        _workingSettings.RpcSettings.DefaultDownloadDir = _txtDownloadDir.Text.Trim();
        _workingSettings.PollingIntervalMs = (int)_numPollInterval.Value;

        // Copy mappings from BindingList back to settings
        _workingSettings.PathMappings.Clear();
        _workingSettings.PathMappings.AddRange(_mappingsBindingList);

        // Save to file
        SettingsManager.Instance.UpdateSettings(_workingSettings);

        _lblStatus.Text = "? Settings saved successfully!";
        _lblStatus.ForeColor = Color.Green;

        MessageBox.Show(
            "Settings saved successfully!\n\n" +
            "Note: Some changes (like polling interval) may require restarting the app.",
            "Settings Saved",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information
        );

        DialogResult = DialogResult.OK;
        Close();
    }

    private AppSettings CloneSettings(AppSettings original)
    {
        return new AppSettings
        {
            RpcSettings = new RpcSettings
            {
                RpcUrl = original.RpcSettings.RpcUrl,
                WsUrl = original.RpcSettings.WsUrl,
                Token = original.RpcSettings.Token,
                DefaultDownloadDir = original.RpcSettings.DefaultDownloadDir
            },
            PathMappings = original.PathMappings.Select(m => new PathMapping
            {
                LinuxPrefix = m.LinuxPrefix,
                WindowsPrefix = m.WindowsPrefix,
                Description = m.Description
            }).ToList(),
            PollingIntervalMs = original.PollingIntervalMs,
            Version = original.Version
        };
    }
}

/// <summary>
/// Dialog for adding/editing a single path mapping
/// </summary>
internal sealed class PathMappingDialog : Form
{
    private TextBox _txtLinuxPath = null!;
    private TextBox _txtWindowsPath = null!;
    private TextBox _txtDescription = null!;
    private Button _btnBrowseWindows = null!;

    public PathMapping? Mapping { get; private set; }

    public PathMappingDialog(PathMapping? existingMapping)
    {
        Text = existingMapping == null ? "Add Path Mapping" : "Edit Path Mapping";
        Width = 650;
        Height = 280;
        MinimumSize = new Size(550, 250);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);
        AutoScaleMode = AutoScaleMode.Font;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 4,
            Padding = new Padding(12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Linux Path
        layout.Controls.Add(new Label { Text = "Linux Path:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 3, 3) }, 0, 0);
        _txtLinuxPath = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "/share/downloads" };
        layout.Controls.Add(_txtLinuxPath, 1, 0);
        layout.SetColumnSpan(_txtLinuxPath, 2);

        // Windows Path
        layout.Controls.Add(new Label { Text = "Windows Path:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 3, 3) }, 0, 1);
        _txtWindowsPath = new TextBox { Dock = DockStyle.Fill, PlaceholderText = @"X:\Downloads" };
        layout.Controls.Add(_txtWindowsPath, 1, 1);

        _btnBrowseWindows = new Button { Text = "Browse...", AutoSize = true };
        _btnBrowseWindows.Click += (s, e) =>
        {
            using var dlg = new FolderBrowserDialog { Description = "Select Windows folder for this mapping" };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _txtWindowsPath.Text = dlg.SelectedPath;
            }
        };
        layout.Controls.Add(_btnBrowseWindows, 2, 1);

        // Description
        layout.Controls.Add(new Label { Text = "Description:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 3, 3) }, 0, 2);
        _txtDescription = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Optional label for this mapping" };
        layout.Controls.Add(_txtDescription, 1, 2);
        layout.SetColumnSpan(_txtDescription, 2);

        // Buttons
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };

        var btnCancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        var btnOk = new Button { Text = "OK", AutoSize = true, Margin = new Padding(8, 0, 0, 0) };
        btnOk.Click += BtnOk_Click;

        btnPanel.Controls.AddRange(new Control[] { btnCancel, btnOk });
        layout.Controls.Add(btnPanel, 0, 3);
        layout.SetColumnSpan(btnPanel, 3);

        Controls.Add(layout);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        // Load existing values
        if (existingMapping != null)
        {
            _txtLinuxPath.Text = existingMapping.LinuxPrefix;
            _txtWindowsPath.Text = existingMapping.WindowsPrefix;
            _txtDescription.Text = existingMapping.Description;
        }
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtLinuxPath.Text))
        {
            MessageBox.Show("Linux path is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtLinuxPath.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(_txtWindowsPath.Text))
        {
            MessageBox.Show("Windows path is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtWindowsPath.Focus();
            return;
        }

        Mapping = new PathMapping
        {
            LinuxPrefix = _txtLinuxPath.Text.Trim(),
            WindowsPrefix = _txtWindowsPath.Text.Trim(),
            Description = _txtDescription.Text.Trim()
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}
