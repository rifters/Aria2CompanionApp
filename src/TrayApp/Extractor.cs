using SharpCompress.Archives;
using SharpCompress.Common;

namespace TrayApp;

/// <summary>
/// Extracts .zip and .rar archives after download completes.
/// </summary>
internal static class Extractor
{
    private static readonly string[] SupportedExtensions = [".zip", ".rar", ".7z", ".tar", ".gz", ".bz2"];

    /// <summary>
    /// If <paramref name="filePath"/> is a supported archive, extracts it next to the original file.
    /// </summary>
    public static void TryExtract(string filePath)
    {
        if (!File.Exists(filePath)) return;

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (!SupportedExtensions.Contains(ext)) return;

        var outputDir = Path.Combine(
            Path.GetDirectoryName(filePath) ?? string.Empty,
            Path.GetFileNameWithoutExtension(filePath));

        try
        {
            Directory.CreateDirectory(outputDir);

            using var archive = ArchiveFactory.Open(filePath);
            foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
            {
                entry.WriteToDirectory(outputDir, new ExtractionOptions
                {
                    ExtractFullPath = true,
                    Overwrite = true
                });
            }

            Notifications.ShowInfo("Extraction Complete",
                $"Extracted to: {Path.GetFileName(outputDir)}");
        }
        catch (Exception ex)
        {
            Notifications.ShowInfo("Extraction Failed", ex.Message);
        }
    }
}
