using Aria2Client;
using Aria2Client.Models;

namespace TrayApp;

/// <summary>
/// Polls Aria2 periodically and maintains an in-memory snapshot of downloads.
/// </summary>
internal sealed class DownloadManager : IDisposable
{
    private readonly Aria2RpcClient _rpc;
    private CancellationTokenSource? _cts;
    private Task? _pollTask;
    private bool _disposed;

    public List<DownloadInfo> ActiveDownloads { get; } = [];
    public List<DownloadInfo> WaitingDownloads { get; } = [];
    public List<DownloadInfo> StoppedDownloads { get; } = [];
    public GlobalStat? GlobalStat { get; private set; }

    public event EventHandler? Updated;

    public DownloadManager(Aria2RpcClient rpc)
    {
        _rpc = rpc;
    }

    public void StartPolling()
    {
        _cts = new CancellationTokenSource();
        _pollTask = PollLoopAsync(_cts.Token);
    }

    public void StopPolling()
    {
        _cts?.Cancel();
    }

    // ──────────────────────────────────────────────

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RefreshAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Swallow transient network errors; will retry next cycle
            }

            await Task.Delay(Settings.PollIntervalMs, ct).ConfigureAwait(false);
        }
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var active = await _rpc.TellActiveAsync(ct);
        var waiting = await _rpc.TellWaitingAsync(0, 100, ct);
        var stopped = await _rpc.TellStoppedAsync(0, 100, ct);
        var stat = await _rpc.GetGlobalStatAsync(ct);

        lock (ActiveDownloads)
        {
            ActiveDownloads.Clear();
            ActiveDownloads.AddRange(active);
        }
        lock (WaitingDownloads)
        {
            WaitingDownloads.Clear();
            WaitingDownloads.AddRange(waiting);
        }
        lock (StoppedDownloads)
        {
            StoppedDownloads.Clear();
            StoppedDownloads.AddRange(stopped);
        }
        GlobalStat = stat;

        Updated?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _pollTask?.Dispose();
    }
}
