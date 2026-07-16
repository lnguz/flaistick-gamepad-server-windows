using System.ServiceProcess;

namespace GamepadServer;

static class VigemCheck
{
    public static bool IsDriverInstalled()
    {
        try
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == "ViGEmBus");
        }
        catch
        {
            return true; // don't block startup if the check itself fails
        }
    }
}
