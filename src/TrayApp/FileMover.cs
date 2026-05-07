namespace TrayApp;

/// <summary>
/// Shows a dialog after a download completes, asking where to move the file.
/// </summary>
internal static class FileMover
{
    public static void ShowMoveDialog(string sourcePath)
    {
        if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
            return;

        using var dlg = new FileMoverDialog(sourcePath);
        dlg.ShowDialog();
    }
}

internal sealed class FileMoverDialog : Form
{
    private readonly string _sourcePath;

    public FileMoverDialog(string sourcePath)
    {
        _sourcePath = sourcePath;
        Text = "Move Download";
        Width = 420;
        Height = 280;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

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

        var btnMovies = MakeButton("🎬  Movies", Settings.MoviesFolder, new Point(12, 90));
        var btnTv = MakeButton("📺  TV Shows", Settings.TvFolder, new Point(12, 130));
        var btnAnime = MakeButton("🎌  Anime", Settings.AnimeFolder, new Point(12, 170));
        var btnDownloads = MakeButton("📁  Downloads", Settings.DownloadsFolder, new Point(12, 210));

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

        Controls.AddRange([label, btnMovies, btnTv, btnAnime, btnDownloads, btnCustom, btnCancel]);
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
            Directory.CreateDirectory(destinationFolder);
            var destPath = Path.Combine(destinationFolder, Path.GetFileName(_sourcePath));

            if (File.Exists(destPath) || Directory.Exists(destPath))
            {
                MessageBox.Show(
                    $"A file with the same name already exists at the destination:\n\n{destPath}",
                    "File Already Exists", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (File.Exists(_sourcePath))
                File.Move(_sourcePath, destPath);
            else if (Directory.Exists(_sourcePath))
                Directory.Move(_sourcePath, destPath);

            MessageBox.Show($"Moved to:\n{destPath}", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Failed to move file:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show($"Access denied:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void MoveToCustomFolder()
    {
        using var dlg = new FolderBrowserDialog { Description = "Choose destination folder" };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            MoveFile(dlg.SelectedPath);
    }
}
