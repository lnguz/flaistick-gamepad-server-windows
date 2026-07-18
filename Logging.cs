using System.Text;

namespace GamepadServer;

/// This app is a WinExe (GUI subsystem) so Console output is never actually attached to a
/// terminal, even under `dotnet run` — mirror every Console.WriteLine to a file instead so
/// diagnostics are actually visible.
static class Logging
{
    public static void Init()
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlaiStick");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "server.log");
            var fileWriter = new StreamWriter(path, append: false) { AutoFlush = true };
            Console.SetOut(new TeeWriter(Console.Out, fileWriter));
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] log file: {path}");
        }
        catch
        {
            // Logging is best-effort; never let it take down the app.
        }
    }

    private sealed class TeeWriter(TextWriter a, TextWriter b) : TextWriter
    {
        public override Encoding Encoding => a.Encoding;
        public override void Write(string? value) { a.Write(value); b.Write(value); }
        public override void WriteLine(string? value) { a.WriteLine(value); b.WriteLine(value); }
        public override void Flush() { a.Flush(); b.Flush(); }
    }
}
