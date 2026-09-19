// SleepGuard - Windows power profile tray utility.

namespace SleepGuard;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            NativeTray.ShowError("SleepGuard requires Windows 11.");
            return 1;
        }

        // The smoke test uses an in-memory power backend and does not change Windows settings.
        if (args.Length == 1 && args[0] == "--self-test")
        {
            return NativeSmokeTest.Run();
        }

        using var mutex = new Mutex(true, @"Local\SleepGuard.67A5D9BD", out var firstInstance);
        if (!firstInstance)
        {
            return 0;
        }

        try
        {
            using var tray = new NativeTray(new PowerManager(new WindowsPowerApi()));
            return tray.Run();
        }
        catch (Exception error)
        {
            NativeTray.ShowError($"SleepGuard could not start.\n\n{error.Message}");
            return 1;
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }
}
