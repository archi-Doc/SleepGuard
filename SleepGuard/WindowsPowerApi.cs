// SleepGuard - Windows power profile tray utility.

using System.Runtime.InteropServices;

namespace SleepGuard;

internal sealed partial class WindowsPowerApi : IPowerApi
{
    private static readonly Guid SleepGroup = new("238c9fa8-0aad-41ed-83f4-97be242c8f20");
    private static readonly Guid SleepTimeout = new("29f6c1db-86da-48c5-9fdb-f2b67b1f44da");
    private static readonly Guid ButtonsGroup = new("4f971e89-eebd-4455-a8de-9e59040e7347");
    private static readonly Guid LidAction = new("5ca83367-6e45-459f-a27b-476b1d01c936");

    public PowerSnapshot Read()
    {
        Check(Native.PowerGetActiveScheme(0, out var pointer), "Read the active power plan");
        Guid active;
        try
        {
            active = Marshal.PtrToStructure<Guid>(pointer);
        }
        finally
        {
            Native.LocalFree(pointer);
        }

        Check(Native.PowerGetUserConfiguredDCPowerMode(out var batteryMode), "Read the battery power mode");
        Check(Native.PowerGetUserConfiguredACPowerMode(out var pluggedInMode), "Read the plugged-in power mode");
        var values = new PowerValues(
            ReadValue(false, SleepGroup, SleepTimeout),
            ReadValue(true, SleepGroup, SleepTimeout),
            ReadValue(false, ButtonsGroup, LidAction),
            ReadValue(true, ButtonsGroup, LidAction));
        return new PowerSnapshot(active, values, batteryMode, pluggedInMode);
    }

    public void WriteValues(PowerValues values)
    {
        // Power API sleep timeouts use seconds. Lid actions: 0 = do nothing, 1 = sleep.
        // Attempt each write so a denied setting cannot prevent restoring the others.
        var errors = new List<string>();
        Attempt(false, SleepGroup, SleepTimeout, values.BatterySleep);
        Attempt(true, SleepGroup, SleepTimeout, values.PluggedInSleep);
        Attempt(false, ButtonsGroup, LidAction, values.BatteryLid);
        Attempt(true, ButtonsGroup, LidAction, values.PluggedInLid);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join("\n", errors));
        }

        void Attempt(bool ac, Guid group, Guid setting, uint value)
        {
            try
            {
                WriteValue(ac, group, setting, value);
            }
            catch (Exception error)
            {
                errors.Add(error.Message);
            }
        }
    }

    public void ActivateScheme(Guid scheme) =>
        Check(Native.PowerSetActiveScheme(0, in scheme), "Activate the power plan");

    public void SetBatteryMode(Guid mode) =>
        Check(Native.PowerSetUserConfiguredDCPowerMode(in mode), "Set the battery power mode");

    public void SetPluggedInMode(Guid mode) =>
        Check(Native.PowerSetUserConfiguredACPowerMode(in mode), "Set the plugged-in power mode");

    private static uint ReadValue(bool ac, Guid group, Guid setting)
    {
        uint value;
        var result = ac
            ? Native.PowerReadACValueIndex(0, in PowerManager.BalancedScheme, in group, in setting, out value)
            : Native.PowerReadDCValueIndex(0, in PowerManager.BalancedScheme, in group, in setting, out value);
        Check(result, "Read a Balanced plan setting");
        return value;
    }

    private static void WriteValue(bool ac, Guid group, Guid setting, uint value)
    {
        var result = ac
            ? Native.PowerWriteACValueIndex(0, in PowerManager.BalancedScheme, in group, in setting, value)
            : Native.PowerWriteDCValueIndex(0, in PowerManager.BalancedScheme, in group, in setting, value);
        Check(result, "Write a Balanced plan setting");
    }

    private static void Check(uint result, string operation)
    {
        if (result == 0)
        {
            return;
        }

        var detail = result switch
        {
            5 => "Access denied. Run SleepGuard as administrator. A managed device policy may also block this setting.",
            2 => "The required power plan or setting was not found.",
            50 => "This power setting is not supported by this device.",
            _ => "Windows could not complete the power operation.",
        };

        // Keep messages in English even when Windows uses another display language.
        throw new InvalidOperationException($"{operation} failed (Windows error {result}). {detail}");
    }
}
