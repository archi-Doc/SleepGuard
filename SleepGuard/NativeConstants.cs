// SleepGuard - Windows power profile tray utility.

namespace SleepGuard;

internal static class NativeConstants
{
    // Window messages and notification-area callbacks.
    internal const uint WindowMessageNull = 0x0000;
    internal const uint WindowMessageDestroy = 0x0002;
    internal const uint WindowMessageClose = 0x0010;
    internal const uint WindowMessageContextMenu = 0x007B;
    internal const uint WindowMessageCommand = 0x0111;
    internal const uint TrayCallbackMessage = 0x8001;
    internal const uint TraySelect = 0x0400;
    internal const uint TrayKeyboardSelect = 0x0401;

    // Menu command identifiers and flags.
    internal const uint NormalCommand = 1;
    internal const uint AwakeCommand = 2;
    internal const uint ExitCommand = 3;
    internal const uint MenuByCommand = 0x0000;
    internal const uint MenuString = 0x0000;
    internal const uint MenuChecked = 0x0008;
    internal const uint MenuByPosition = 0x0400;
    internal const uint MenuSeparator = 0x0800;
    internal const uint PopupRightButton = 0x0002;
    internal const uint PopupReturnCommand = 0x0100;

    // Shell notification-area operations and flags.
    internal const uint NotifyAdd = 0;
    internal const uint NotifyModify = 1;
    internal const uint NotifyDelete = 2;
    internal const uint NotifySetVersion = 4;
    internal const uint NotifyMessage = 0x0001;
    internal const uint NotifyIcon = 0x0002;
    internal const uint NotifyTip = 0x0004;
    internal const uint NotifyShowTip = 0x0080;
    internal const uint NotifyVersion4 = 4;

    internal const uint MessageBoxError = 0x0010;
    internal const int SmallIconWidth = 49;
    internal const int DpiAwarenessPerMonitorV2 = -4;
    internal const uint IconResourceVersion = 0x00030000;
}
