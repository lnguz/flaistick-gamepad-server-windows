using System.Windows.Forms;

namespace GamepadServer;

sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _icon;
    private readonly Action _onExit;

    public TrayApp(Action onExit)
    {
        _onExit = onExit;

        var menu = new ContextMenuStrip();
        menu.Items.Add("FlaiStick Gamepad Server", null, (_, _) => { }).Enabled = false;
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());

        var appIcon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath)
                      ?? System.Drawing.SystemIcons.Application;

        _icon = new NotifyIcon
        {
            Icon = appIcon,
            Text = "FlaiStick Gamepad Server",
            ContextMenuStrip = menu,
            Visible = true
        };
    }

    private void ExitApplication()
    {
        _icon.Visible = false;
        _onExit();
        ExitThread();
    }
}
