namespace TrayApp;

/// <summary>
/// Hard-coded configuration for this personal tool.
/// All values here are intentionally embedded for convenience as a local-only app.
/// </summary>
internal static class Settings
{
    public const string RpcUrl = "http://192.168.4.120:6800/jsonrpc";
    public const string WsUrl = "ws://192.168.4.120:6800/jsonrpc";
    public const string Token = "7iQzqgg6Soe5MqpWGaLODhAbNcFwkFEaQYCQhCAtDrIZ";

    public static readonly string MoviesFolder = @"\\NAS\Media\Movies";
    public static readonly string TvFolder = @"\\NAS\Media\TV";
    public static readonly string AnimeFolder = @"\\NAS\Media\Anime";
    public static readonly string DownloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";

    public const int PollIntervalMs = 3000;
}
