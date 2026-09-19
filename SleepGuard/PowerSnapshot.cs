// SleepGuard - Windows power profile tray utility.

namespace SleepGuard;

internal readonly record struct PowerSnapshot(
    Guid ActiveScheme,
    PowerValues Values,
    Guid BatteryMode,
    Guid PluggedInMode);
