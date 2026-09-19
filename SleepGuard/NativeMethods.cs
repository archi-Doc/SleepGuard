// SleepGuard - Windows power profile tray utility.

using System.Runtime.InteropServices;

namespace SleepGuard;

internal static unsafe partial class NativeMethods
{
    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint GetModuleHandle(string? name);

    [LibraryImport("user32.dll", EntryPoint = "RegisterClassExW", SetLastError = true)]
    internal static partial ushort RegisterClassEx(in WindowClass windowClass);

    [LibraryImport("user32.dll", EntryPoint = "UnregisterClassW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int UnregisterClass(string name, nint instance);

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial nint CreateWindowEx(
        uint exStyle,
        string className,
        string title,
        uint style,
        int x,
        int y,
        int width,
        int height,
        nint parent,
        nint menu,
        nint instance,
        nint parameter);

    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")]
    internal static partial nint DefWindowProc(nint window, uint message, nuint wParam, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "GetMessageW", SetLastError = true)]
    internal static partial int GetMessage(out Message message, nint window, uint min, uint max);

    [LibraryImport("user32.dll")]
    internal static partial int TranslateMessage(in Message message);

    [LibraryImport("user32.dll", EntryPoint = "DispatchMessageW")]
    internal static partial nint DispatchMessage(in Message message);

    [LibraryImport("user32.dll", EntryPoint = "PostMessageW")]
    internal static partial int PostMessage(nint window, uint message, nuint wParam, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    internal static partial nint SendMessage(nint window, uint message, nuint wParam, nint lParam);

    [LibraryImport("user32.dll")]
    internal static partial void PostQuitMessage(int exitCode);

    [LibraryImport("user32.dll")]
    internal static partial int DestroyWindow(nint window);

    [LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint RegisterWindowMessage(string name);

    [LibraryImport("shell32.dll", EntryPoint = "Shell_NotifyIconW")]
    internal static partial int ShellNotifyIcon(uint operation, in NotifyIconData data);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial nint CreatePopupMenu();

    [LibraryImport("user32.dll", EntryPoint = "AppendMenuW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial int AppendMenu(nint menu, uint flags, nuint id, string? label);

    [LibraryImport("user32.dll")]
    internal static partial int CheckMenuRadioItem(nint menu, uint first, uint last, uint selected, uint flags);

    [LibraryImport("user32.dll")]
    internal static partial uint TrackPopupMenu(
        nint menu,
        uint flags,
        int x,
        int y,
        int reserved,
        nint window,
        nint rectangle);

    [LibraryImport("user32.dll")]
    internal static partial int DestroyMenu(nint menu);

    [LibraryImport("user32.dll")]
    internal static partial int SetForegroundWindow(nint window);

    [LibraryImport("user32.dll")]
    internal static partial int GetCursorPos(out Point point);

    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int MessageBox(nint window, string text, string title, uint flags);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial nint CreateIconFromResourceEx(
        byte* bits,
        uint size,
        int icon,
        uint version,
        int width,
        int height,
        uint flags);

    [LibraryImport("user32.dll")]
    internal static partial int DestroyIcon(nint icon);

    [LibraryImport("user32.dll")]
    internal static partial int GetSystemMetrics(int index);

    [LibraryImport("user32.dll")]
    internal static partial int SetProcessDpiAwarenessContext(nint context);

    [LibraryImport("user32.dll")]
    internal static partial int GetMenuItemCount(nint menu);

    [LibraryImport("user32.dll")]
    internal static partial uint GetMenuItemID(nint menu, int position);

    [LibraryImport("user32.dll")]
    internal static partial uint GetMenuState(nint menu, uint id, uint flags);
}
