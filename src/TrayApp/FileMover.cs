namespace TrayApp;
using TrayApp.Models;

/// <summary>
/// Shows a dialog after a download completes, asking where to move the file.
/// </summary>
internal static class FileMover
{
    public static bool ShowMoveDialog(string sourcePath)
    {
        try
        {
            // Always show the dialog, even if the path doesn't exist locally
            // The user can then navigate to find the file
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                MessageBox.Show("No file path available for this download.", 
                    "Cannot Move File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Try to map the path from Aria2 server path to Windows path
            var windowsPath = PathMapper.ToWindowsPath(sourcePath);

            // Check if it's a Linux/server path that needs translation
            if (!File.Exists(windowsPath) && !Directory.Exists(windowsPath))
            {
                var mappingConfigured = windowsPath != sourcePath && windowsPath != sourcePath.Replace('/', '\\');

                var message = mappingConfigured
                    ? $"The download path from Aria2 server:\n\n{sourcePath}\n\n" +
                      $"Was mapped to:\n\n{windowsPath}\n\n" +
                      "But this path doesn't exist on your local system.\n\n" +
                      "Would you like to browse for the file manually?"
                    : $"The download path from Aria2 server:\n\n{sourcePath}\n\n" +
                      "This path doesn't exist on your local system.\n\n" +
                      "Tip: You can configure path mappings via:\n" +
                      "Right-click tray icon → Settings → Path Mappings\n\n" +
                      "Would you like to browse for the file manually?";

                var result = MessageBox.Show(
                    message,
                    "File Not Found",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    return ShowBrowseAndMoveDialog(Path.GetFileName(sourcePath));
                }
                return false;
            }

            using var dlg = new FileMoverDialog(windowsPath);
            return dlg.ShowDialog() == DialogResult.OK;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error showing move dialog:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                "Move File Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            return false;
        }
    }

    private static bool ShowBrowseAndMoveDialog(string expectedFileName)
    {
        try
        {
            using var openDlg = new OpenFileDialog
            {
                Title = $"Locate downloaded file: {expectedFileName}",
                Filter = "All files (*.*)|*.*",
                FileName = expectedFileName
            };

            var result = openDlg.ShowDialog();

            // User clicked OK and selected a file
            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(openDlg.FileName))
            {
                using var moveDlg = new FileMoverDialog(openDlg.FileName);
                return moveDlg.ShowDialog() == DialogResult.OK;
            }
            // User clicked Cancel or closed the dialog
            return false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error in browse dialog:\n\n{ex.Message}",
                "Browse Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            return false;
        }
    }
}

internal sealed class FileMoverDialog : Form
{
    private readonly string _sourcePath;

    public FileMoverDialog(string sourcePath)
    {
        _sourcePath = sourcePath;
        Text = "Move Download";
        Width = 500;
        Height = 350;
        MinimumSize = new Size(450, 300);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);
        AutoScaleMode = AutoScaleMode.Font;

        BuildUi();
    }

    private void BuildUi()
    {
        var fileName = Path.GetFileName(_sourcePath);
        var presets = SettingsManager.Instance.Settings.MovePresets;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = Math.Max(4, presets.Count + 2), // Label + presets + custom + cancel
            Padding = new Padding(12),
            AutoSize = true
        };

        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        var label = new Label
        {
            Text = presets.Count > 0 
                ? $"Download complete:\n{fileName}\n\nMove to:"
                : $"Download complete:\n{fileName}\n\nNo presets configured yet.\nUse '➕ Add Preset' or '📂 Custom Folder' to get started!",
            AutoSize = true,
            Dock = DockStyle.Fill,
            MaximumSize = new Size(450, 0)
        };
        mainPanel.Controls.Add(label, 0, 0);
        mainPanel.SetColumnSpan(label, 2);

        // Add preset buttons (if any configured)
        int row = 1;
        foreach (var preset in presets.Take(6)) // Max 6 presets to avoid huge dialog
        {
            var btn = MakeButton(preset.Label, preset.Path);
            int col = row == 1 ? 0 : (row - 1) % 2;
            mainPanel.Controls.Add(btn, col, row);

            if (col == 1) row++;
        }

        // Add Preset button
        var btnAddPreset = new Button
        {
            Text = "➕ Add Preset",
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(3),
            BackColor = Color.LightGreen
        };
        btnAddPreset.Click += (_, _) => AddNewPreset();
        mainPanel.Controls.Add(btnAddPreset, 0, row);

        // Custom folder button
        var btnCustom = new Button
        {
            Text = "📂  Custom Folder…",
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(3)
        };
        btnCustom.Click += (_, _) => MoveToCustomFolder();
        mainPanel.Controls.Add(btnCustom, 1, row);

        // Cancel button
        var btnCancel = new Button
        {
            Text = "Leave in place",
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(3),
            DialogResult = DialogResult.Cancel
        };
        mainPanel.Controls.Add(btnCancel, 0, row + 1);
        mainPanel.SetColumnSpan(btnCancel, 2);

        Controls.Add(mainPanel);
        CancelButton = btnCancel;
    }

    private Button MakeButton(string text, string destination)
    {
        var btn = new Button
        {
            Text = text,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(3)
        };
        btn.Click += (_, _) => MoveFile(destination, promptForPreset: false); // Preset button - don't ask
        return btn;
    }

    private void MoveFile(string destinationFolder, bool promptForPreset = true)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(destinationFolder))
            {
                MessageBox.Show("Destination folder is not configured.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(destinationFolder))
            {
                var result = MessageBox.Show(
                    $"The destination folder does not exist:\n\n{destinationFolder}\n\nCreate it?",
                    "Create Folder?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                    Directory.CreateDirectory(destinationFolder);
                else
                    return;
            }

            var destPath = Path.Combine(destinationFolder, Path.GetFileName(_sourcePath));

            if (File.Exists(destPath) || Directory.Exists(destPath))
            {
                MessageBox.Show(
                    $"A file with the same name already exists at the destination:\n\n{destPath}",
                    "File Already Exists", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(_sourcePath) && !Directory.Exists(_sourcePath))
            {
                MessageBox.Show(
                    $"Source file not found:\n\n{_sourcePath}\n\nIt may have been moved or deleted.",
                    "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            if (File.Exists(_sourcePath))
                File.Move(_sourcePath, destPath);
            else if (Directory.Exists(_sourcePath))
                Directory.Move(_sourcePath, destPath);

            // Only ask to save as preset if user browsed for a custom folder
            if (promptForPreset)
                PromptSaveAsPreset(destinationFolder);

            // Don't show success message here - let the caller handle it
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Failed to move file:\n\n{ex.Message}\n\nSource: {_sourcePath}\nDestination: {destinationFolder}", 
                "I/O Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show($"Access denied:\n\n{ex.Message}\n\nMake sure you have permissions to write to:\n{destinationFolder}", 
                "Permission Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unexpected error:\n\n{ex.Message}\n\nType: {ex.GetType().Name}", 
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AddNewPreset()
    {
        using var dlg = new FolderBrowserDialog { Description = "Choose folder for new preset" };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        var folderPath = dlg.SelectedPath;

        // Ask for preset label
        var labelDialog = new Form
        {
            Text = "New Preset",
            ClientSize = new Size(450, 150),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.Sizable,
            MaximizeBox = false,
            MinimizeBox = false,
            MinimumSize = new Size(400, 180)
        };

        var lblPrompt = new Label
        {
            Text = $"Enter a label for this preset:",
            AutoSize = true,
            Location = new Point(12, 12),
            MaximumSize = new Size(410, 0)
        };

        var lblPath = new Label
        {
            Text = folderPath,
            AutoSize = true,
            Location = new Point(12, lblPrompt.Bottom + 4),
            MaximumSize = new Size(410, 0),
            ForeColor = Color.Gray,
            Font = new Font(lblPrompt.Font.FontFamily, 8)
        };

        var txtLabel = new TextBox
        {
            Width = 410,
            Location = new Point(12, lblPath.Bottom + 12),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            PlaceholderText = "e.g., Movies, TV Shows, Books..."
        };

        var btnOk = new Button
        {
            Text = "Save Preset",
            DialogResult = DialogResult.OK,
            Location = new Point(labelDialog.ClientSize.Width - 200, txtLabel.Bottom + 16),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Width = 100
        };

        var btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(labelDialog.ClientSize.Width - 90, txtLabel.Bottom + 16),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Width = 80
        };

        labelDialog.Controls.AddRange(new Control[] { lblPrompt, lblPath, txtLabel, btnOk, btnCancel });
        labelDialog.AcceptButton = btnOk;
        labelDialog.CancelButton = btnCancel;
        labelDialog.ActiveControl = txtLabel; // Focus the textbox

        if (labelDialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(txtLabel.Text))
        {
            var newPreset = new MovePreset
            {
                Label = txtLabel.Text.Trim(),
                Path = folderPath
            };

            var settings = SettingsManager.Instance.Settings;
            settings.MovePresets.Add(newPreset);
            SettingsManager.Instance.Save();

            MessageBox.Show($"✓ Preset '{newPreset.Label}' added!\n\nIt will appear in the Move dialog next time.",
                "Preset Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void PromptSaveAsPreset(string folderPath)
    {
        // Check if this folder is already a preset
        var settings = SettingsManager.Instance.Settings;
        if (settings.MovePresets.Any(p => p.Path.Equals(folderPath, StringComparison.OrdinalIgnoreCase)))
            return; // Already a preset

        var result = MessageBox.Show(
            $"Save this folder as a preset?\n\n{folderPath}\n\nIt will appear as a button next time.",
            "Save as Preset?",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (result != DialogResult.Yes)
            return;

        // Ask for label
        var labelDialog = new Form
        {
            Text = "Preset Label",
            ClientSize = new Size(450, 120), // Use ClientSize instead of Width/Height
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.Sizable,
            MaximizeBox = false,
            MinimizeBox = false,
            MinimumSize = new Size(400, 150)
        };

        var lblPrompt = new Label
        {
            Text = "Enter a label for this preset:",
            AutoSize = true,
            Location = new Point(12, 12)
        };

        var txtLabel = new TextBox
        {
            Width = 410,
            Location = new Point(12, lblPrompt.Bottom + 12),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            PlaceholderText = Path.GetFileName(folderPath), // Suggest folder name
            Text = Path.GetFileName(folderPath) // Pre-fill with suggestion
        };
        txtLabel.SelectAll(); // Select all text so user can type over it

        var btnOk = new Button
        {
            Text = "Save",
            DialogResult = DialogResult.OK,
            Location = new Point(labelDialog.ClientSize.Width - 180, txtLabel.Bottom + 16),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Width = 80
        };

        var btnCancel = new Button
        {
            Text = "Skip",
            DialogResult = DialogResult.Cancel,
            Location = new Point(labelDialog.ClientSize.Width - 90, txtLabel.Bottom + 16),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Width = 80
        };

        labelDialog.Controls.AddRange(new Control[] { lblPrompt, txtLabel, btnOk, btnCancel });
        labelDialog.AcceptButton = btnOk;
        labelDialog.CancelButton = btnCancel;
        labelDialog.ActiveControl = txtLabel; // Focus the textbox

        if (labelDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txtLabel.Text))
        {
            var newPreset = new MovePreset
            {
                Label = txtLabel.Text.Trim(),
                Path = folderPath
            };

            settings.MovePresets.Add(newPreset);
            SettingsManager.Instance.Save();
        }
    }

    private void MoveToCustomFolder()
    {
        using var dlg = new FolderBrowserDialog { Description = "Choose destination folder" };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            MoveFile(dlg.SelectedPath);
    }
}
