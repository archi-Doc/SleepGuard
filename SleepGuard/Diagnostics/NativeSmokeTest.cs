// SleepGuard - Windows power profile tray utility.

using static SleepGuard.NativeConstants;
using static SleepGuard.NativeMethods;

namespace SleepGuard;

internal static class NativeSmokeTest
{
    internal static int Run()
    {
        try
        {
            var backend = new TestPowerApi();
            var errors = 0;
            using var tray = new NativeTray(new PowerManager(backend), _ => errors++);
            Require(tray.Profile == PowerProfile.Awake, "Initial profile");
            Require(backend.State.Values == new PowerValues(300, 18000, 0, 0), "Initial Awake values");
            VerifyMenu(tray, AwakeCommand);

            // Exercise the real unmanaged callback using the same command dispatch as the menu.
            SendMessage(tray.Window, WindowMessageCommand, NormalCommand, 0);
            Require(tray.Profile == PowerProfile.Normal, "Normal selection");
            Require(backend.State.Values == new PowerValues(300, 300, 1, 1), "Normal values");
            VerifyMenu(tray, NormalCommand);
            SendMessage(tray.Window, WindowMessageCommand, AwakeCommand, 0);
            Require(tray.Profile == PowerProfile.Awake, "Awake selection");
            Require(backend.State.Values == new PowerValues(300, 18000, 0, 0), "Awake values");
            VerifyMenu(tray, AwakeCommand);
            backend.FailWrites = true;
            SendMessage(tray.Window, WindowMessageCommand, ExitCommand, 0);
            Require(errors == 1 && tray.Profile is null, "Failed Exit stays running");
            VerifyMenu(tray, 0);
            backend.FailWrites = false;

            // The message loop must process Exit, restore Normal, and receive WM_QUIT.
            PostMessage(tray.Window, WindowMessageCommand, ExitCommand, 0);
            Require(tray.Run() == 0, "Message loop exit");
            var expected = new PowerSnapshot(
                PowerManager.BalancedScheme,
                new(300, 300, 1, 1),
                Guid.Empty,
                Guid.Empty);
            Require(backend.State == expected, "Exit restores Normal");
            Console.WriteLine("PASS: Native icon, menu, callbacks, mode changes, failed Exit, and clean Exit. No real power settings changed.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"FAIL: {error.Message}");
            return 1;
        }
    }

    private static void VerifyMenu(NativeTray tray, uint selected)
    {
        var menu = tray.CreateMenu();
        try
        {
            Require(GetMenuItemCount(menu) == 4, "Menu count");
            Require(
                GetMenuItemID(menu, 0) == 1 && GetMenuItemID(menu, 1) == 2 && GetMenuItemID(menu, 3) == 3,
                "Menu order");
            Require((GetMenuState(menu, 2, MenuByPosition) & MenuSeparator) != 0, "Menu separator");

            var normalChecked = (GetMenuState(menu, NormalCommand, MenuByCommand) & MenuChecked) != 0;
            var awakeChecked = (GetMenuState(menu, AwakeCommand, MenuByCommand) & MenuChecked) != 0;
            Require(normalChecked == (selected == NormalCommand), "Normal radio state");
            Require(awakeChecked == (selected == AwakeCommand), "Awake radio state");
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    private static void Require(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }
    }

    private sealed class TestPowerApi : IPowerApi
    {
        internal PowerSnapshot State { get; private set; } = new(PowerManager.BalancedScheme, new(60, 60, 2, 2), Guid.Empty, Guid.Empty);

        internal bool FailWrites
        {
            get;
            set;
        }

        public PowerSnapshot Read() => this.State;

        public void WriteValues(PowerValues values)
        {
            if (this.FailWrites)
            {
                throw new InvalidOperationException("Injected test failure.");
            }

            this.State = this.State with
            {
                Values = values,
            };
        }

        public void ActivateScheme(Guid scheme) => this.State = this.State with { ActiveScheme = scheme };

        public void SetBatteryMode(Guid mode) => this.State = this.State with { BatteryMode = mode };

        public void SetPluggedInMode(Guid mode) => this.State = this.State with { PluggedInMode = mode };
    }
}
