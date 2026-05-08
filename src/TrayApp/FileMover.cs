namespace TrayApp;

/// <summary>
/// Shows a dialog after a download completes, asking where to move the file.
/// </summary>
internal static class FileMover
{
    public static void ShowMoveDialog(string sourcePath)
    {
        // Always show the dialog, even if the path doesn't exist locally
        // The user can then navigate to find the file
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            MessageBox.Show("No file path available for this download.", 
                "Cannot Move File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
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
                ShowBrowseAndMoveDialog(Path.GetFileName(sourcePath));
            }
            return;
        }

        using var dlg = new FileMoverDialog(windowsPath);
        dlg.ShowDialog();
    }

    private static void ShowBrowseAndMoveDialog(string expectedFileName)
    {
        using var openDlg = new OpenFileDialog
        {
            Title = $"Locate downloaded file: {expectedFileName}",
            Filter = "All files (*.*)|*.*",
            FileName = expectedFileName
        };

        if (openDlg.ShowDialog() == DialogResult.OK)
        {
            using var moveDlg = new FileMoverDialog(openDlg.FileName);
            moveDlg.ShowDialog();
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

        var label = new Label
        {
            Text = $"Download complete:\n{fileName}\n\nMove to:",
            AutoSize = true,
            Location = new Point(12, 12),
            MaximumSize = new Size(380, 0)
        };

        // Preset folder buttons - use common Windows locations
        var downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        var videosFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        var documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var musicFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

        var btnDownloads = MakeButton("📁  Downloads", downloadsFolder, new Point(12, 90));
        var btnVideos = MakeButton("🎬  Videos", videosFolder, new Point(12, 130));
        var btnDocuments = MakeButton("📄  Documents", documentsFolder, new Point(12, 170));
        var btnMusic = MakeButton("🎵  Music", musicFolder, new Point(12, 210));

        var btnCustom = new Button
        {
            Text = "📂  Custom Folder…",
            Width = 180,
            Height = 30,
            Location = new Point(210, 90)
        };
        btnCustom.Click += (_, _) => MoveToCustomFolder();

        var btnCancel = new Button
        {
            Text = "Leave in place",
            Width = 180,
            Height = 30,
            Location = new Point(210, 130),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange([label, btnDownloads, btnVideos, btnDocuments, btnMusic, btnCustom, btnCancel]);
        CancelButton = btnCancel;
    }

    private Button MakeButton(string text, string destination, Point location)
    {
        var btn = new Button
        {
            Text = text,
            Width = 180,
            Height = 30,
            Location = location
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

            MessageBox.Show($"✓ Moved successfully to:\n\n{destPath}", "Done", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
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
