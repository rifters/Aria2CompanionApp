using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Aria2Client.Models;

namespace Aria2Client;

/// <summary>
/// Listens to aria2 WebSocket notifications and raises events
/// for download start, complete, and error.
/// </summary>
public class Aria2WebSocketClient : IDisposable
{
    private readonly string _wsUrl;
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public event EventHandler<string>? DownloadStarted;
    public event EventHandler<string>? DownloadComplete;
    public event EventHandler<string>? DownloadError;
    public event EventHandler<string>? DownloadPaused;
    public event EventHandler<string>? DownloadStopped;
    public event EventHandler? Connected;
    public event EventHandler<Exception>? Disconnected;

    public bool IsConnected => _ws?.State == WebSocketState.Open;

    public Aria2WebSocketClient(string wsUrl)
    {
        _wsUrl = wsUrl;
    }

    /// <summary>Connects to the aria2 WebSocket endpoint and starts listening.</summary>
    public async Task ConnectAsync(CancellationToken externalCt = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
        await ConnectAndListenAsync(_cts.Token);
    }

    /// <summary>Starts the WebSocket listener in a background loop with auto-reconnect.</summary>
    public void StartBackground(CancellationToken externalCt = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
        _ = RunLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    // ──────────────────────────────────────────────
    //  Internal
    // ──────────────────────────────────────────────

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ConnectAndListenAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Disconnected?.Invoke(this, ex);
                // Wait before reconnecting
                await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
            }
        }
    }

    private async Task ConnectAndListenAsync(CancellationToken ct)
    {
        _ws?.Dispose();
        _ws = new ClientWebSocket();

        await _ws.ConnectAsync(new Uri(_wsUrl), ct);
        Connected?.Invoke(this, EventArgs.Empty);

        var buffer = new byte[65536];

        while (_ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var result = await _ws.ReceiveAsync(buffer, ct);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closed", ct);
                break;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                ProcessMessage(json);
            }
        }
    }

    private void ProcessMessage(string json)
    {
        try
        {
            var notification = JsonSerializer.Deserialize<WebSocketNotification>(json);
            if (notification is null) return;

            var gid = notification.Params?.FirstOrDefault()?.Gid ?? string.Empty;

            switch (notification.Method)
            {
                case "aria2.onDownloadStart":
                    DownloadStarted?.Invoke(this, gid);
                    break;
                case "aria2.onDownloadComplete":
                    DownloadComplete?.Invoke(this, gid);
                    break;
                case "aria2.onDownloadError":
                    DownloadError?.Invoke(this, gid);
                    break;
                case "aria2.onDownloadPause":
                    DownloadPaused?.Invoke(this, gid);
                    break;
                case "aria2.onDownloadStop":
                    DownloadStopped?.Invoke(this, gid);
                    break;
                case "aria2.onBtDownloadComplete":
                    DownloadComplete?.Invoke(this, gid);
                    break;
            }
        }
        catch
        {
            // Swallow parse errors for malformed messages
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _ws?.Dispose();
        GC.SuppressFinalize(this);
    }
}
