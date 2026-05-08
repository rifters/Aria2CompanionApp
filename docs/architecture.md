# Aria2 Companion App – Architecture

## Overview

A lightweight Windows tray application built with C# (.NET 8 / WinForms) that provides a user-friendly front-end for an Aria2 daemon running on a NAS.

```
Aria2CompanionApp/
├── Aria2Companion.sln
└── src/
    ├── Aria2Client/           # Class library – Aria2 communication
    │   ├── Aria2Client.csproj
    │   ├── Aria2RpcClient.cs      # HTTP JSON-RPC calls
    │   ├── Aria2WebSocketClient.cs # WebSocket event listener
    │   └── Models/
    │       └── Aria2Models.cs     # Request / response models
    └── TrayApp/               # WinForms application
        ├── TrayApp.csproj
        ├── Program.cs             # Entry point
        ├── Settings.cs            # Hard-coded config (RPC URL, paths, token)
        ├── TrayIcon.cs            # NotifyIcon + context menu + ApplicationContext
        ├── Notifications.cs       # Balloon-tip helpers
        ├── DownloadManager.cs     # Polling loop, in-memory state
        ├── FileMover.cs           # Post-download move dialog
        ├── Extractor.cs           # Auto-extract .zip/.rar via SharpCompress
        ├── ClipboardWatcher.cs    # Magnet/URL clipboard detection
        └── UI/
            ├── DownloadsWindow.cs # Main downloads list (tabs: Active/Waiting/Completed)
            └── AddUrlDialog.cs    # Manual URL / drag-and-drop input
```

## Component Responsibilities

| Component | Responsibility |
|---|---|
| `Aria2RpcClient` | Wraps all Aria2 JSON-RPC methods over HTTP |
| `Aria2WebSocketClient` | Long-running WebSocket connection; fires C# events for download state changes |
| `DownloadManager` | Polls the RPC every 3 s; holds `ActiveDownloads`, `WaitingDownloads`, `StoppedDownloads` |
| `TrayIcon` | Hosts the `NotifyIcon`, context menu, and wires all subsystems together |
| `Notifications` | Thin wrapper around `NotifyIcon.ShowBalloonTip` |
| `FileMover` | Displays a dialog asking where to move the completed file |
| `Extractor` | Uses **SharpCompress** to unpack archives automatically |
| `ClipboardWatcher` | Uses `WM_CLIPBOARDUPDATE` to detect clipboard-pasted magnets/URLs |
| `DownloadsWindow` | Three-tab `ListView` showing live download data |
| `AddUrlDialog` | Text input + drag-and-drop to add URLs/magnets |

## Data Flow

```
[Aria2 Daemon on NAS]
      │
      ├─ HTTP JSON-RPC  ──────►  Aria2RpcClient  ──►  DownloadManager (poll)
      │                                                       │
      └─ WebSocket events ────►  Aria2WebSocketClient        │
                │                     │                       │
                │              DownloadComplete               │
                │                     │                       │
                │              FileMover.ShowMoveDialog       │
                │              Extractor.TryExtract           │
                │                                             │
                └─────────────────────────────────────────►  TrayIcon
                                                                │
                                                         Notifications
                                                         DownloadsWindow
```

## Settings

All settings are hard-coded in `Settings.cs`:

| Setting | Value |
|---|---|
| RPC URL | `http://192.168.4.120:6800/jsonrpc` |
| WebSocket URL | `ws://192.168.4.120:6800/jsonrpc` |
| Token | see `Settings.cs` |
| Movies folder | `\\NAS\Media\Movies` |
| TV folder | `\\NAS\Media\TV` |
| Anime folder | `\\NAS\Media\Anime` |
| Downloads folder | `%USERPROFILE%\Downloads` |
| Poll interval | 3 000 ms |

## Dependencies

| Package | Purpose |
|---|---|
| `SharpCompress` | Extract .zip, .rar, .7z, .tar archives |
| Built-in `System.Net.Http.HttpClient` | HTTP JSON-RPC |
| Built-in `System.Net.WebSockets.ClientWebSocket` | WebSocket event stream |
| Built-in `System.IO.Compression` | (fallback, via SharpCompress) |
