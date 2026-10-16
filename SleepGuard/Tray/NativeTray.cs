// SleepGuard - Windows power profile tray utility.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static SleepGuard.NativeConstants;
using static SleepGuard.NativeMethods;

namespace SleepGuard;

internal sealed unsafe class NativeTray : IDisposable
{
    private const string ClassName = "SleepGuard.TrayWindow";
    private static NativeTray? current;
    private readonly PowerManager power;
    private readonly Action<string> reportError;
    private readonly nint instance = GetModuleHandle(null);
    private readonly uint taskbarCreated = RegisterWindowMessage("TaskbarCreated");
    private nint window;
    private nint icon;
    private bool registered;
    private bool trayAdded;
    private bool busy;
    private PowerProfile? profile;

    internal NativeTray(PowerManager power, Action<string>? reportError = null)
    {
        this.power = power;
        this.reportError = reportError ?? ShowError;
        current = this;
        try
        {
            SetProcessDpiAwarenessContext(DpiAwarenessPerMonitorV2);
            this.icon = TrayIconLoader.Load();
            fixed (char* name = ClassName)
            {
                var windowClass = new WindowClass
                {
                    Size = (uint)sizeof(WindowClass),
                    Procedure = &WindowProcedure,
                    Instance = this.instance,
                    ClassName = name,
                    Icon = this.icon,
                    SmallIcon = this.icon,
                };
                Ensure(RegisterClassEx(in windowClass) != 0, "Register the tray window");
            }

            this.registered = true;

            // A hidden top-level window receives Explorer restart broadcasts.
            this.window = CreateWindowEx(0, ClassName, "SleepGuard", 0, 0, 0, 0, 0, 0, 0, this.instance, 0);
            Ensure(this.window != 0, "Create the tray window");
            this.AddTrayIcon();
            this.Apply(PowerProfile.Awake);
        }
        catch
        {
            this.Dispose();
            throw;
        }
    }

    internal nint Window => this.window;

    internal PowerProfile? Profile => this.profile;

    public void Dispose()
    {
        if (this.trayAdded)
        {
            var data = this.IconData();
            ShellNotifyIcon(NotifyDelete, in data);
            this.trayAdded = false;
        }

        if (this.window != 0)
        {
            DestroyWindow(this.window);
            this.window = 0;
        }

        if (this.registered)
        {
            UnregisterClass(ClassName, this.instance);
            this.registered = false;
        }

        if (this.icon != 0)
        {
            DestroyIcon(this.icon);
            this.icon = 0;
        }

        if (current == this)
        {
            current = null;
        }
    }

    internal static void ShowError(string message) => MessageBox(0, message, "SleepGuard", MessageBoxError);

    internal int Run()
    {
        int result;
        while ((result = GetMessage(out var message, 0, 0, 0)) > 0)
        {
            TranslateMessage(in message);
            DispatchMessage(in message);
        }

        Ensure(result >= 0, "Read a window message");
        return 0;
    }

    internal bool Apply(PowerProfile requested)
    {
        try
        {
            this.power.Apply(requested);
            this.profile = requested;
            this.UpdateTrayIcon();
            return true;
        }
        catch (Exception error)
        {
            this.profile = null;
            this.UpdateTrayIcon();
            this.reportError($"Could not apply {requested} mode.\n\n{error.Message}\n\nSleepGuard will remain running. Select a mode to retry.");
            return false;
        }
    }

    internal nint CreateMenu()
    {
        var menu = CreatePopupMenu();
        Ensure(menu != 0, "Create the tray menu");
        try
        {
            Ensure(AppendMenu(menu, MenuString, NormalCommand, "&1: Normal") != 0, "Add the Normal command");
            Ensure(AppendMenu(menu, MenuString, AwakeCommand, "&2: Awake") != 0, "Add the Awake command");
            Ensure(AppendMenu(menu, MenuSeparator, 0, null) != 0, "Add the menu separator");
            Ensure(AppendMenu(menu, MenuString, ExitCommand, "&3: Exit") != 0, "Add the Exit command");
            if (this.profile.HasValue)
            {
                var selected = this.profile == PowerProfile.Normal ? NormalCommand : AwakeCommand;
                CheckMenuRadioItem(menu, NormalCommand, AwakeCommand, selected, MenuByCommand);
            }

            return menu;
        }
        catch
        {
            DestroyMenu(menu);
            throw;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint WindowProcedure(nint handle, uint message, nuint wParam, nint lParam)
    {
        try
        {
            var app = current;
            if (app is not null)
            {
                if (message == TrayCallbackMessage)
                {
                    var notification = (uint)((nuint)lParam & 0xFFFF);
                    if (notification is TraySelect or TrayKeyboardSelect or WindowMessageContextMenu)
                    {
                        app.ShowMenu(wParam);
                    }

                    return 0;
                }

                if (message == WindowMessageCommand)
                {
                    app.ExecuteCommand((uint)(wParam & 0xFFFF));
                    return 0;
                }

                if (message == WindowMessageClose)
                {
                    app.ExecuteCommand(ExitCommand);
                    return 0;
                }

                if (message == WindowMessageDestroy)
                {
                    PostQuitMessage(0);
                    return 0;
                }

                if (message == app.taskbarCreated && app.taskbarCreated != 0)
                {
                    app.trayAdded = false;
                    app.AddTrayIcon();
                    return 0;
                }
            }
        }
        catch (Exception error)
        {
            // Managed exceptions must never cross the unmanaged window procedure boundary.
            ShowError($"SleepGuard could not complete the operation.\n\n{error.Message}");
        }

        return DefWindowProc(handle, message, wParam, lParam);
    }

    private static void Ensure(bool success, string operation)
    {
        if (!success)
        {
            throw new InvalidOperationException($"{operation} failed (Windows error {Marshal.GetLastPInvokeError()}).");
        }
    }

    private void ExecuteCommand(uint command)
    {
        if (this.busy)
        {
            return;
        }

        this.busy = true;
        try
        {
            switch (command)
            {
                case NormalCommand:
                    this.Apply(PowerProfile.Normal);
                    break;
                case AwakeCommand:
                    this.Apply(PowerProfile.Awake);
                    break;
                case ExitCommand:
                    // Exit is conditional on successfully restoring and verifying Normal.
                    if (this.Apply(PowerProfile.Normal))
                    {
                        DestroyWindow(this.window);
                    }

                    break;
            }
        }
        finally
        {
            this.busy = false;
        }
    }

    private void ShowMenu(nuint coordinates)
    {
        if (this.busy)
        {
            return;
        }

        int x = (short)(coordinates & 0xFFFF);
        int y = (short)((coordinates >> 16) & 0xFFFF);
        if (x == -1 && y == -1 && GetCursorPos(out var position) != 0)
        {
            x = position.X;
            y = position.Y;
        }

        var menu = this.CreateMenu();
        uint command;
        try
        {
            SetForegroundWindow(this.window);
            command = TrackPopupMenu(menu, PopupReturnCommand | PopupRightButton, x, y, 0, this.window, 0);
            PostMessage(this.window, WindowMessageNull, 0, 0);
        }
        finally
        {
            DestroyMenu(menu);
        }

        this.ExecuteCommand(command);
    }

    private NotifyIconData IconData()
    {
        var data = new NotifyIconData
        {
            Size = (uint)sizeof(NotifyIconData),
            Window = this.window,
            Id = 1,
            Flags = NotifyMessage | NotifyIcon | NotifyTip | NotifyShowTip,
            CallbackMessage = TrayCallbackMessage,
            Icon = this.icon,
            Version = NotifyVersion4,
        };
        var tip = this.profile.HasValue ? $"SleepGuard - {this.profile}" : "SleepGuard - Settings error";
        for (var i = 0; i < tip.Length; i++)
        {
            data.Tip[i] = tip[i];
        }

        return data;
    }

    private void AddTrayIcon()
    {
        var data = this.IconData();
        Ensure(ShellNotifyIcon(NotifyAdd, in data) != 0, "Add the tray icon");
        this.trayAdded = true;
        Ensure(ShellNotifyIcon(NotifySetVersion, in data) != 0, "Enable tray keyboard and mouse support");
    }

    private void UpdateTrayIcon()
    {
        if (!this.trayAdded)
        {
            return;
        }

        var data = this.IconData();
        ShellNotifyIcon(NotifyModify, in data);
    }
}
