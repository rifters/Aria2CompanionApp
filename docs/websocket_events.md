# Aria2 WebSocket Events

Aria2 pushes download state changes over the same WebSocket connection used for JSON-RPC.

## Connection

Connect to: `ws://<host>:<port>/jsonrpc`

No initial handshake is needed. Once connected, aria2 sends notifications automatically.

## Notification Format

```json
{
  "jsonrpc": "2.0",
  "method": "aria2.onDownloadStart",
  "params": [{ "gid": "2089b05ecca3d829" }]
}
```

## Events

### `aria2.onDownloadStart`

Fired when a download begins.

**Payload:** `[{ "gid": "<gid>" }]`

---

### `aria2.onDownloadPause`

Fired when a download is paused.

**Payload:** `[{ "gid": "<gid>" }]`

---

### `aria2.onDownloadStop`

Fired when a download is stopped manually.

**Payload:** `[{ "gid": "<gid>" }]`

---

### `aria2.onDownloadComplete`

Fired when a download finishes successfully (HTTP/FTP/Metalink).

**Payload:** `[{ "gid": "<gid>" }]`

---

### `aria2.onDownloadError`

Fired when a download fails.

**Payload:** `[{ "gid": "<gid>" }]`

---

### `aria2.onBtDownloadComplete`

Fired when a BitTorrent download finishes (all pieces downloaded; seeding may continue).

**Payload:** `[{ "gid": "<gid>" }]`

---

## Reconnection Strategy

`Aria2WebSocketClient` uses an automatic reconnect loop:

1. Connect to the WebSocket endpoint.
2. If the connection drops, wait 5 seconds, then reconnect.
3. Loop until the `CancellationToken` is cancelled.

## Usage in Code

```csharp
var ws = new Aria2WebSocketClient("ws://192.168.4.120:6800/jsonrpc");

ws.DownloadStarted  += (_, gid) => Console.WriteLine($"Started:  {gid}");
ws.DownloadComplete += (_, gid) => Console.WriteLine($"Complete: {gid}");
ws.DownloadError    += (_, gid) => Console.WriteLine($"Error:    {gid}");

ws.StartBackground(); // non-blocking, runs on a background Task
```
