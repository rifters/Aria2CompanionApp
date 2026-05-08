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
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
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
        => AddUriAsync(uri, null, ct);

    /// <summary>Adds a URI with options (e.g., download directory).</summary>
    public async Task<string> AddUriAsync(string uri, Dictionary<string, object>? options, CancellationToken ct = default)
    {
        // Build params according to Aria2 spec: [token, [uris], options]
        var uris = new[] { uri };

        object[] extraParams;
        if (options == null || options.Count == 0)
        {
            extraParams = new object[] { uris };
        }
        else
        {
            // Include the options dictionary as the third parameter
            extraParams = new object[] { uris, options };
        }

        return await CallAsync<string>("aria2.addUri", ct, extraParams);
    }

    /// <summary>Returns active downloads.</summary>
    public async Task<DownloadInfo[]> TellActiveAsync(CancellationToken ct = default)
        => await CallAsync<DownloadInfo[]>("aria2.tellActive", ct) ?? [];

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

    /// <summary>Removes a completed/error/removed download from memory.</summary>
    public Task<string> RemoveDownloadResultAsync(string gid, CancellationToken ct = default)
        => CallAsync<string>("aria2.removeDownloadResult", ct, gid);

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

        try
        {
            var requestJson = JsonSerializer.Serialize(request, JsonOptions);

            // Use StringContent directly to ensure proper encoding
            var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(_rpcUrl, content, ct);

            // Get the response body first for better error messages
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Aria2 server returned HTTP {(int)response.StatusCode} ({response.StatusCode}). " +
                    $"Method: {method}. " +
                    $"Response: {(body.Length > 200 ? body.Substring(0, 200) + "..." : body)}");
            }

            var result = JsonSerializer.Deserialize<Aria2Response<T>>(body, JsonOptions)
                ?? throw new InvalidOperationException("Empty response from aria2");

            if (result.Error is not null)
                throw new Aria2RpcException(result.Error.Code, result.Error.Message);

            return result.Result!;
        }
        catch (HttpRequestException)
        {
            throw; // Re-throw HTTP exceptions as-is
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException("Request was cancelled", ct);
        }
        catch (TaskCanceledException)
        {
            throw new TimeoutException($"Request to Aria2 timed out after {_http.Timeout.TotalSeconds} seconds");
        }
        catch (Exception ex) when (ex is not Aria2RpcException)
        {
            throw new InvalidOperationException($"Failed to communicate with Aria2: {ex.Message}", ex);
        }
    }
}

/// <summary>Represents an error returned by the Aria2 JSON-RPC API.</summary>
public class Aria2RpcException(int code, string message)
    : Exception($"Aria2 RPC error {code}: {message}")
{
    public int Code { get; } = code;
}
