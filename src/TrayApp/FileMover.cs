namespace TrayApp;

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

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(12),
            AutoSize = true
        };

        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        var label = new Label
        {
            Text = $"Download complete:\n{fileName}\n\nMove to:",
            AutoSize = true,
            Dock = DockStyle.Fill,
            MaximumSize = new Size(450, 0)
        };
        mainPanel.Controls.Add(label, 0, 0);
        mainPanel.SetColumnSpan(label, 2);

        // Preset folder buttons - use common Windows locations
        var downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        var videosFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        var documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var musicFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

        var btnDownloads = MakeButton("📁  Downloads", downloadsFolder);
        var btnVideos = MakeButton("🎬  Videos", videosFolder);
        var btnDocuments = MakeButton("📄  Documents", documentsFolder);
        var btnMusic = MakeButton("🎵  Music", musicFolder);

        var btnCustom = new Button
        {
            Text = "📂  Custom Folder…",
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(3)
        };
        btnCustom.Click += (_, _) => MoveToCustomFolder();

        var btnCancel = new Button
        {
            Text = "Leave in place",
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(3),
            DialogResult = DialogResult.Cancel
        };

        mainPanel.Controls.Add(btnDownloads, 0, 1);
        mainPanel.Controls.Add(btnCustom, 1, 1);
        mainPanel.Controls.Add(btnVideos, 0, 2);
        mainPanel.Controls.Add(btnCancel, 1, 2);
        mainPanel.Controls.Add(btnDocuments, 0, 3);
        mainPanel.Controls.Add(btnMusic, 0, 4);

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
        btn.Click += (_, _) => MoveFile(destination);
        return btn;
    }

    private void MoveFile(string destinationFolder)
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

    private void MoveToCustomFolder()
    {
        using var dlg = new FolderBrowserDialog { Description = "Choose destination folder" };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            MoveFile(dlg.SelectedPath);
    }
}
