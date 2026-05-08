using Aria2Client;

namespace TrayApp;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        ApplicationConfiguration.Initialize();

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => 
        {
            // Don't show connection errors - they're expected when Aria2 is offline
            if (e.Exception is HttpRequestException || 
                e.Exception is System.Net.Sockets.SocketException ||
                e.Exception?.InnerException is HttpRequestException ||
                e.Exception?.InnerException is System.Net.Sockets.SocketException)
            {
                System.Diagnostics.Debug.WriteLine($"Connection error (expected if Aria2 offline): {e.Exception.Message}");
                return;
            }

            MessageBox.Show($"An error occurred:\n\n{e.Exception.Message}\n\nStack trace:\n{e.Exception.StackTrace}", 
                "Aria2 Companion Error", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show($"An unhandled error occurred:\n\n{ex?.Message}\n\nStack trace:\n{ex?.StackTrace}", 
                "Aria2 Companion Fatal Error", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
        };

        try
        {
            // Initialize settings manager (loads settings.json or creates default)
            var settings = SettingsManager.Instance.Settings;

            var rpcClient = new Aria2RpcClient(settings.RpcSettings.RpcUrl, settings.RpcSettings.Token);
            var wsClient = new Aria2WebSocketClient(settings.RpcSettings.WsUrl);
            var downloadManager = new DownloadManager(rpcClient);
            var trayApp = new TrayIcon(rpcClient, wsClient, downloadManager);

            Application.Run(trayApp);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start Aria2 Companion:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                "Aria2 Companion Startup Error", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Error);
        }
    }
}
