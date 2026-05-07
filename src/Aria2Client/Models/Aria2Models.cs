using System.Text.Json.Serialization;

namespace Aria2Client.Models;

public class Aria2Request
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public object[] Params { get; set; } = [];

    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
}

public class Aria2Response<T>
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public T? Result { get; set; }

    [JsonPropertyName("error")]
    public Aria2Error? Error { get; set; }
}

public class Aria2Error
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class DownloadInfo
{
    [JsonPropertyName("gid")]
    public string Gid { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("totalLength")]
    public string TotalLength { get; set; } = "0";

    [JsonPropertyName("completedLength")]
    public string CompletedLength { get; set; } = "0";

    [JsonPropertyName("downloadSpeed")]
    public string DownloadSpeed { get; set; } = "0";

    [JsonPropertyName("uploadSpeed")]
    public string UploadSpeed { get; set; } = "0";

    [JsonPropertyName("numSeeders")]
    public string NumSeeders { get; set; } = "0";

    [JsonPropertyName("connections")]
    public string Connections { get; set; } = "0";

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("files")]
    public FileInfo[] Files { get; set; } = [];

    [JsonPropertyName("bittorrent")]
    public BitTorrentInfo? BitTorrent { get; set; }

    [JsonIgnore]
    public long TotalLengthBytes => long.TryParse(TotalLength, out var v) ? v : 0;

    [JsonIgnore]
    public long CompletedLengthBytes => long.TryParse(CompletedLength, out var v) ? v : 0;

    [JsonIgnore]
    public long DownloadSpeedBytes => long.TryParse(DownloadSpeed, out var v) ? v : 0;

    [JsonIgnore]
    public double Progress => TotalLengthBytes > 0
        ? (double)CompletedLengthBytes / TotalLengthBytes * 100.0
        : 0.0;

    [JsonIgnore]
    public string Name
    {
        get
        {
            if (BitTorrent?.Info?.Name is { Length: > 0 } torrentName)
                return torrentName;
            if (Files is { Length: > 0 } && Files[0].Path is { Length: > 0 } filePath)
                return Path.GetFileName(filePath);
            return Gid;
        }
    }

    [JsonIgnore]
    public TimeSpan? Eta
    {
        get
        {
            if (DownloadSpeedBytes <= 0 || TotalLengthBytes <= 0)
                return null;
            var remaining = TotalLengthBytes - CompletedLengthBytes;
            return TimeSpan.FromSeconds((double)remaining / DownloadSpeedBytes);
        }
    }
}

public class FileInfo
{
    [JsonPropertyName("index")]
    public string Index { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("length")]
    public string Length { get; set; } = "0";

    [JsonPropertyName("completedLength")]
    public string CompletedLength { get; set; } = "0";

    [JsonPropertyName("selected")]
    public string Selected { get; set; } = "true";

    [JsonPropertyName("uris")]
    public UriInfo[] Uris { get; set; } = [];
}

public class UriInfo
{
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class BitTorrentInfo
{
    [JsonPropertyName("info")]
    public BitTorrentInfoDetail? Info { get; set; }

    [JsonPropertyName("announceList")]
    public string[][]? AnnounceList { get; set; }

    [JsonPropertyName("mode")]
    public string? Mode { get; set; }
}

public class BitTorrentInfoDetail
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class GlobalStat
{
    [JsonPropertyName("downloadSpeed")]
    public string DownloadSpeed { get; set; } = "0";

    [JsonPropertyName("uploadSpeed")]
    public string UploadSpeed { get; set; } = "0";

    [JsonPropertyName("numActive")]
    public string NumActive { get; set; } = "0";

    [JsonPropertyName("numWaiting")]
    public string NumWaiting { get; set; } = "0";

    [JsonPropertyName("numStopped")]
    public string NumStopped { get; set; } = "0";

    [JsonPropertyName("numStoppedTotal")]
    public string NumStoppedTotal { get; set; } = "0";

    [JsonIgnore]
    public long DownloadSpeedBytes => long.TryParse(DownloadSpeed, out var v) ? v : 0;

    [JsonIgnore]
    public long UploadSpeedBytes => long.TryParse(UploadSpeed, out var v) ? v : 0;
}

public class AddUriResult
{
    // The result is a GID string directly
}

public class WebSocketNotification
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = string.Empty;

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public NotificationParam[]? Params { get; set; }
}

public class NotificationParam
{
    [JsonPropertyName("gid")]
    public string Gid { get; set; } = string.Empty;
}
