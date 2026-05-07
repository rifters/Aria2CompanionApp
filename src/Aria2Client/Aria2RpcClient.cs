using System.Net.Http.Json;
using System.Text.Json;
using Aria2Client.Models;

namespace Aria2Client;

/// <summary>
/// JSON-RPC client for the Aria2 daemon.
/// </summary>
public class Aria2RpcClient
{
    private readonly HttpClient _http;
    private readonly string _rpcUrl;
    private readonly string _token;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Aria2RpcClient(string rpcUrl, string token)
    {
        _rpcUrl = rpcUrl;
        _token = token;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    }

    // ──────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────

    /// <summary>Adds a URI (URL or magnet link) to the download queue.</summary>
    public Task<string> AddUriAsync(string uri, CancellationToken ct = default)
        => CallAsync<string>("aria2.addUri", ct, new[] { uri });

    /// <summary>Returns active downloads.</summary>
    public Task<DownloadInfo[]> TellActiveAsync(CancellationToken ct = default)
        => CallAsync<DownloadInfo[]>("aria2.tellActive", ct) ?? Task.FromResult(Array.Empty<DownloadInfo>());

    /// <summary>Returns waiting downloads (offset 0, limit 100).</summary>
    public Task<DownloadInfo[]> TellWaitingAsync(int offset = 0, int num = 100, CancellationToken ct = default)
        => CallAsync<DownloadInfo[]>("aria2.tellWaiting", ct, offset, num);

    /// <summary>Returns stopped (completed/error) downloads.</summary>
    public Task<DownloadInfo[]> TellStoppedAsync(int offset = 0, int num = 100, CancellationToken ct = default)
        => CallAsync<DownloadInfo[]>("aria2.tellStopped", ct, offset, num);

    /// <summary>Returns info for a specific GID.</summary>
    public Task<DownloadInfo> TellStatusAsync(string gid, CancellationToken ct = default)
        => CallAsync<DownloadInfo>("aria2.tellStatus", ct, gid);

    /// <summary>Removes a download by GID.</summary>
    public Task<string> RemoveAsync(string gid, CancellationToken ct = default)
        => CallAsync<string>("aria2.remove", ct, gid);

    /// <summary>Forcibly removes a download by GID (even if active).</summary>
    public Task<string> ForceRemoveAsync(string gid, CancellationToken ct = default)
        => CallAsync<string>("aria2.forceRemove", ct, gid);

    /// <summary>Pauses an active download.</summary>
    public Task<string> PauseAsync(string gid, CancellationToken ct = default)
        => CallAsync<string>("aria2.pause", ct, gid);

    /// <summary>Resumes a paused download.</summary>
    public Task<string> UnpauseAsync(string gid, CancellationToken ct = default)
        => CallAsync<string>("aria2.unpause", ct, gid);

    /// <summary>Returns global download/upload statistics.</summary>
    public Task<GlobalStat> GetGlobalStatAsync(CancellationToken ct = default)
        => CallAsync<GlobalStat>("aria2.getGlobalStat", ct);

    /// <summary>Returns aria2 version info.</summary>
    public Task<JsonElement> GetVersionAsync(CancellationToken ct = default)
        => CallAsync<JsonElement>("aria2.getVersion", ct);

    // ──────────────────────────────────────────────
    //  Core RPC helper
    // ──────────────────────────────────────────────

    private async Task<T> CallAsync<T>(string method, CancellationToken ct, params object[] extraParams)
    {
        var allParams = new object[extraParams.Length + 1];
        allParams[0] = $"token:{_token}";
        Array.Copy(extraParams, 0, allParams, 1, extraParams.Length);

        var request = new Aria2Request { Method = method, Params = allParams };
        var response = await _http.PostAsJsonAsync(_rpcUrl, request, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<Aria2Response<T>>(body, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from aria2");

        if (result.Error is not null)
            throw new Aria2RpcException(result.Error.Code, result.Error.Message);

        return result.Result!;
    }
}

/// <summary>Represents an error returned by the Aria2 JSON-RPC API.</summary>
public class Aria2RpcException(int code, string message)
    : Exception($"Aria2 RPC error {code}: {message}")
{
    public int Code { get; } = code;
}
