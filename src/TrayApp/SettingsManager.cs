using System.Text.Json;
using System.Text.Json;
using TrayApp.Models;

namespace TrayApp;

/// <summary>
/// Manages application settings stored in settings.json.
/// Provides thread-safe singleton access to configuration.
/// </summary>
internal sealed class SettingsManager
{
    private static readonly Lazy<SettingsManager> _instance = new(() => new SettingsManager());
    private static readonly string _settingsPath = Path.Combine(
        AppContext.BaseDirectory,
        "settings.json"
    );

    private readonly object _lock = new();
    private AppSettings _settings = null!;

    public static SettingsManager Instance => _instance.Value;
    public AppSettings Settings
    {
        get
        {
            lock (_lock)
            {
                return _settings;
            }
        }
    }

    private SettingsManager()
    {
        Load();
    }

    /// <summary>
    /// Loads settings from settings.json. Creates default file if missing.
    /// </summary>
    public void Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? CreateDefaults();
                }
                else
                {
                    // First run - check for migration from old Settings.cs
                    if (ShouldMigrateOldSettings())
                    {
                        _settings = MigrateFromOldSettings();
                    }
                    else
                    {
                        _settings = CreateDefaults();
                    }
                    Save();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load settings.json. Using defaults.\n\nError: {ex.Message}",
                    "Settings Load Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                _settings = CreateDefaults();
            }
        }
    }

    /// <summary>
    /// Saves current settings to settings.json.
    /// </summary>
    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save settings.json.\n\nError: {ex.Message}",
                    "Settings Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }

    /// <summary>
    /// Updates settings with new values and saves immediately.
    /// </summary>
    public void UpdateSettings(AppSettings newSettings)
    {
        lock (_lock)
        {
            _settings = newSettings;
            Save();
        }
    }

    /// <summary>
    /// Creates default settings for first-time users.
    /// </summary>
    private AppSettings CreateDefaults()
    {
        return new AppSettings
        {
            RpcSettings = new RpcSettings
            {
                RpcUrl = global::TrayApp.Settings.RpcUrl,
                WsUrl = global::TrayApp.Settings.WsUrl,
                Token = global::TrayApp.Settings.Token,
                DefaultDownloadDir = global::TrayApp.Settings.DefaultAria2DownloadDir
            },
            PathMappings = new List<PathMapping>(),
            PollingIntervalMs = global::TrayApp.Settings.PollIntervalMs,
            Version = "1.0"
        };
    }

    /// <summary>
    /// Checks if old Settings.cs has non-default values worth migrating.
    /// </summary>
    private bool ShouldMigrateOldSettings()
    {
        // If Settings.cs has localhost defaults, no need to migrate
        return global::TrayApp.Settings.RpcUrl != "http://localhost:6800/jsonrpc" ||
               !string.IsNullOrEmpty(global::TrayApp.Settings.Token);
    }

    /// <summary>
    /// One-time migration from old hardcoded Settings.cs.
    /// </summary>
    private AppSettings MigrateFromOldSettings()
    {
        var result = MessageBox.Show(
            "Detected configuration in old Settings.cs.\n\n" +
            "Would you like to migrate these settings to the new settings.json file?\n\n" +
            "This is a one-time migration. After migration, all settings will be " +
            "stored in settings.json (which is not committed to Git).",
            "Migrate Settings",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question
        );

        if (result == DialogResult.Yes)
        {
            var migrated = new AppSettings
            {
                RpcSettings = new RpcSettings
                {
                    RpcUrl = global::TrayApp.Settings.RpcUrl,
                    WsUrl = global::TrayApp.Settings.WsUrl,
                    Token = global::TrayApp.Settings.Token,
                    DefaultDownloadDir = global::TrayApp.Settings.DefaultAria2DownloadDir
                },
                PathMappings = new List<PathMapping>(),
                PollingIntervalMs = global::TrayApp.Settings.PollIntervalMs,
                Version = "1.0"
            };

            MessageBox.Show(
                "? Settings migrated successfully!\n\n" +
                "You can now configure additional settings via:\n" +
                "Right-click tray icon ? Settings",
                "Migration Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            return migrated;
        }

        return CreateDefaults();
    }

    /// <summary>
    /// Gets the full path to settings.json
    /// </summary>
    public static string GetSettingsPath() => _settingsPath;
}
