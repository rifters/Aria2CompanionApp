# Aria2 JSON-RPC Reference

All calls are HTTP POST to `http://<host>:<port>/jsonrpc`.

## Request Format

```json
{
  "jsonrpc": "2.0",
  "method": "aria2.addUri",
  "params": ["token:<secret>", ["https://example.com/file.iso"]],
  "id": "req-1"
}
```

## Methods Used

### `aria2.addUri`

Add one or more URIs to the download queue.

**Params:** `[token, [uri, ...], options?]`
**Returns:** GID string

```json
{ "jsonrpc": "2.0", "result": "2089b05ecca3d829", "id": "req-1" }
```

---

### `aria2.tellActive`

List active downloads.

**Params:** `[token, keys?]`
**Returns:** Array of `DownloadInfo`

---

### `aria2.tellWaiting`

List waiting/paused downloads.

**Params:** `[token, offset, num, keys?]`
**Returns:** Array of `DownloadInfo`

---

### `aria2.tellStopped`

List stopped (completed/error) downloads.

**Params:** `[token, offset, num, keys?]`
**Returns:** Array of `DownloadInfo`

---

### `aria2.tellStatus`

Get status for a specific download.

**Params:** `[token, gid, keys?]`
**Returns:** `DownloadInfo`

---

### `aria2.remove`

Remove a download (must be paused or stopped).

**Params:** `[token, gid]`
**Returns:** GID string

---

### `aria2.forceRemove`

Forcibly remove an active download.

**Params:** `[token, gid]`
**Returns:** GID string

---

### `aria2.pause`

Pause an active download.

**Params:** `[token, gid]`
**Returns:** GID string

---

### `aria2.unpause`

Resume a paused download.

**Params:** `[token, gid]`
**Returns:** GID string

---

### `aria2.getGlobalStat`

Get global download/upload statistics.

**Params:** `[token]`
**Returns:**

```json
{
  "downloadSpeed": "102400",
  "uploadSpeed": "1024",
  "numActive": "2",
  "numWaiting": "0",
  "numStopped": "5"
}
```

---

### `aria2.getVersion`

Get aria2 version.

**Params:** `[token]`
**Returns:**

```json
{
  "version": "1.37.0",
  "enabledFeatures": ["BitTorrent", "GZip", "HTTPS", ...]
}
```

## DownloadInfo Fields

| Field | Type | Description |
|---|---|---|
| `gid` | string | Unique download ID |
| `status` | string | `active`, `waiting`, `paused`, `error`, `complete`, `removed` |
| `totalLength` | string | Total file size in bytes |
| `completedLength` | string | Downloaded bytes |
| `downloadSpeed` | string | Download speed in bytes/s |
| `uploadSpeed` | string | Upload speed in bytes/s |
| `numSeeders` | string | (BitTorrent) number of seeders |
| `connections` | string | Number of connections |
| `errorCode` | string | Error code (if errored) |
| `errorMessage` | string | Error message (if errored) |
| `files` | array | Array of file objects |
| `bittorrent` | object | BitTorrent-specific info |

## Error Response

```json
{
  "jsonrpc": "2.0",
  "error": { "code": 1, "message": "Download not found" },
  "id": "req-1"
}
```
