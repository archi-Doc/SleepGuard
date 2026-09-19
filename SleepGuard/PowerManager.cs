// SleepGuard - Windows power profile tray utility.

namespace SleepGuard;

internal sealed class PowerManager(IPowerApi api)
{
    internal static readonly Guid BalancedScheme = new("381b4222-f694-41f0-9685-ff5bb260df2e");
    internal static readonly Guid BalancedMode = Guid.Empty;

    public void Apply(PowerProfile profile)
    {
        var values = PowerValues.For(profile);
        var previous = api.Read();
        try
        {
            api.WriteValues(values);
            api.ActivateScheme(BalancedScheme);
            api.SetBatteryMode(BalancedMode);
            api.SetPluggedInMode(BalancedMode);
            var actual = api.Read();
            if (actual != new PowerSnapshot(BalancedScheme, values, BalancedMode, BalancedMode))
            {
                throw new InvalidOperationException("Windows did not retain the requested power settings.");
            }
        }
        catch (Exception error)
        {
            var restoreErrors = this.RestoreSnapshot(previous);
            var recovery = restoreErrors.Count == 0
                ? "The previous settings were restored."
                : "Some previous settings could not be restored: " + string.Join(" ", restoreErrors);
            throw new InvalidOperationException(error.Message + "\n\n" + recovery, error);
        }
    }

    private static void Restore(Action action, List<string> errors)
    {
        try
        {
            action();
        }
        catch (Exception error)
        {
            errors.Add(error.Message);
        }
    }

    private List<string> RestoreSnapshot(PowerSnapshot snapshot)
    {
        // Restore every setting independently, even if another restore operation fails.
        var errors = new List<string>();
        Restore(() => api.WriteValues(snapshot.Values), errors);
        Restore(() => api.ActivateScheme(snapshot.ActiveScheme), errors);
        Restore(() => api.SetBatteryMode(snapshot.BatteryMode), errors);
        Restore(() => api.SetPluggedInMode(snapshot.PluggedInMode), errors);
        Restore(
            () =>
            {
                if (api.Read() != snapshot)
                {
                    throw new InvalidOperationException("Restored settings could not be verified.");
                }
            },
            errors);
        return errors;
    }
}
