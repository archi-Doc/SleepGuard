// SleepGuard - Windows power profile tray utility.

namespace SleepGuard;

internal readonly record struct PowerValues(
    uint BatterySleep,
    uint PluggedInSleep,
    uint BatteryLid,
    uint PluggedInLid)
{
    private const uint FiveMinutesInSeconds = 5 * 60;
    private const uint FiveHoursInSeconds = 5 * 60 * 60;
    private const uint DoNothingOnLidClose = 0;
    private const uint SleepOnLidClose = 1;

    public static PowerValues For(PowerProfile profile)
    {
        return profile switch
        {
            PowerProfile.Normal => new(
                FiveMinutesInSeconds,
                FiveMinutesInSeconds,
                SleepOnLidClose,
                SleepOnLidClose),
            PowerProfile.Awake => new(
                FiveMinutesInSeconds,
                FiveHoursInSeconds,
                DoNothingOnLidClose,
                DoNothingOnLidClose),
            _ => throw new ArgumentOutOfRangeException(nameof(profile)),
        };
    }
}
