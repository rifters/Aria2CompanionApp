using Aria2Client;

namespace TrayApp;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var rpcClient = new Aria2RpcClient(Settings.RpcUrl, Settings.Token);
        var wsClient = new Aria2WebSocketClient(Settings.WsUrl);
        var downloadManager = new DownloadManager(rpcClient);
        var trayApp = new TrayIcon(rpcClient, wsClient, downloadManager);

        Application.Run(trayApp);
    }
}
