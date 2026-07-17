using System.Diagnostics;
using System.Windows.Forms;

namespace GamepadServer;

internal static class Program
{
    private const int Port = 9000;
    private const int DiscoveryPort = 47998;
    private const string AppName = "GamepadServer";

    [STAThread]
    private static void Main()
    {
        try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal; }
        catch { /* best effort */ }

        StartupRegistration.Register(AppName);

        ControllerHub hub;
        try
        {
            hub = new ControllerHub();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Failed to initialize ViGEmBus. Make sure it's installed (winget install ViGEm.ViGEmBus) " +
                "and that you've restarted this PC after installing it.\n\n" +
                $"Details: {ex.Message}",
                "FlaiStick Gamepad Server",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var cts = new CancellationTokenSource();
        var server = new UdpServer(Port, hub);
        var discovery = new DiscoveryServer(DiscoveryPort, Port);
        _ = Task.Run(() => server.RunAsync(cts.Token));
        _ = Task.Run(() => discovery.RunAsync(cts.Token));

        var tray = new TrayApp(() =>
        {
            cts.Cancel();
            hub.Dispose();
        });

        Application.Run(tray);
    }
}
