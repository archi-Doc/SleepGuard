// SleepGuard - Windows power profile tray utility.

namespace SleepGuard;

internal interface IPowerApi
{
    PowerSnapshot Read();

    void WriteValues(PowerValues values);

    void ActivateScheme(Guid scheme);

    void SetBatteryMode(Guid mode);

    void SetPluggedInMode(Guid mode);
}
