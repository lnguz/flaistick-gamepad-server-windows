using System.Diagnostics;
using Microsoft.Win32;

namespace GamepadServer;

static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void Register(string appName)
    {
        RemoveLegacyRunKeyEntry(appName);

        var exePath = Environment.ProcessPath;
        if (exePath is null) return;

        RunSchTasks(psi =>
        {
            psi.ArgumentList.Add("/Create");
            psi.ArgumentList.Add("/TN");
            psi.ArgumentList.Add(appName);
            psi.ArgumentList.Add("/SC");
            psi.ArgumentList.Add("ONLOGON");
            psi.ArgumentList.Add("/TR");
            psi.ArgumentList.Add($"\"{exePath}\"");
            psi.ArgumentList.Add("/F");
        });
    }

    public static void Unregister(string appName)
    {
        RemoveLegacyRunKeyEntry(appName);

        RunSchTasks(psi =>
        {
            psi.ArgumentList.Add("/Delete");
            psi.ArgumentList.Add("/TN");
            psi.ArgumentList.Add(appName);
            psi.ArgumentList.Add("/F");
        });
    }

    // Cleans up the entry used by older versions of this app, which relied on the
    // classic Run key. That key is only processed by explorer.exe, so it never fires
    // on devices that boot straight into an alternate shell (e.g. the Windows 11 Xbox
    // full-screen experience on handheld gaming PCs) until the user switches to the
    // desktop. A Task Scheduler ONLOGON trigger runs regardless of which shell loads.
    private static void RemoveLegacyRunKeyEntry(string appName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            key?.DeleteValue(appName, throwOnMissingValue: false);
        }
        catch { /* best effort */ }
    }

    private static void RunSchTasks(Action<ProcessStartInfo> configure)
    {
        try
        {
            var psi = new ProcessStartInfo("schtasks.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            configure(psi);
            using var process = Process.Start(psi);
            process?.WaitForExit(5000);
        }
        catch { /* best effort */ }
    }
}
