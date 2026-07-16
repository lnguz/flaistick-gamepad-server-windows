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

        if (!VigemCheck.IsDriverInstalled())
        {
            MessageBox.Show(
                "ViGEmBus was not found on this PC. Virtual controllers won't work until it's installed.\n\n" +
                "Install it with: winget install ViGEm.ViGEmBus\n" +
                "or from https://github.com/ViGEm/ViGEmBus/releases",
                "FlaiStick Gamepad Server",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        var hub = new ControllerHub();
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
