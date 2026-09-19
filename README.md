# SleepGuard

A small C# / native Win32 notification-area application for Windows 11.

![SleepGuard icon](SleepGuard/Assets/SleepGuard.png)

The teal-to-blue shield, crescent moon, and star are original vector artwork.
The EXE and notification area use the same icon, with nine sizes from 16 to 256 pixels.

## Behavior

SleepGuard starts in **Normal** mode without opening a main window. Left-click or
right-click its tray icon to open this menu:

```text
1: Normal
2: Awake
-----------
3: Exit
```

The selected mode has a radio mark. Only one mode can be selected.

| Setting | Normal | Awake |
| --- | --- | --- |
| Power plan | Balanced | Balanced |
| Power mode, battery and plugged in | Balanced | Balanced |
| Sleep after inactivity, battery | 5 minutes | 5 minutes |
| Sleep after inactivity, plugged in | 5 minutes | 5 hours |
| Close lid, battery | Sleep | Do nothing |
| Close lid, plugged in | Sleep | Do nothing |

**Exit** applies and verifies Normal mode before closing. If changing settings
fails, an English error message appears and the application stays running so the
operation can be retried. A failed mode change attempts to restore the settings
that existed immediately before the change. Failed restoration is reported.

The application edits the existing Balanced plan. It does not restore your
pre-launch configuration on Exit; it applies the Normal values above.

## Run

Open `dist/SleepGuard/SleepGuard.exe`. This is a **NativeAOT executable for Windows x64**.
No .NET runtime, installer, or additional icon files are required on the target PC.
The executable contains the compiled application and icon resources.

If Windows hides the icon, look in the notification-area overflow menu.
Starting another instance in the same Windows session exits immediately.
If a setting is denied, close SleepGuard after restoring Normal if possible and
run it as administrator. Organization policies may prevent changes even when
running as administrator.

There are no polling timers, background shell processes, third-party runtime
dependencies, or automatic startup registration. The tray, menu, window message loop,
and power changes use Windows native APIs directly. Explorer restarts automatically
restore the tray icon. This utility changes sleep settings; Awake still allows idle sleep
after the specified delay. Other Windows policies, applications, battery protection,
or hardware behavior can also affect when the device sleeps or hibernates.

Use the Exit menu for a guaranteed checked return to Normal. Forced process
termination, crashes, sign-out, and shutdown cannot guarantee this cleanup. The
next launch applies Normal again.

## Build

Install the .NET 10 SDK and Visual Studio 2022 or later with **Desktop development
with C++**, including the MSVC toolchain and Windows SDK. Then run from the repository
directory:

```powershell
dotnet publish .\SleepGuard\SleepGuard.csproj -c Release -r win-x64 -o .\dist\SleepGuard
```

`PublishAot` is enabled in the project. Publishing compiles native code and performs
trimming/AOT analysis. There is no Windows Forms dependency, dynamic code generation,
or reflection-based UI. Native interop uses source-generated `LibraryImport` stubs
and an `UnmanagedCallersOnly` window callback.

For a regular managed development build without native compilation:

```powershell
dotnet build .\SleepGuard\SleepGuard.csproj -c Debug -p:PublishAot=false
```

Use `win-arm64` instead of `win-x64` with the matching MSVC ARM64 tools when building
for a Windows on ARM device. The supplied executable is x64.

To regenerate the icon assets on Windows:

```powershell
.\tools\Build-Icon.ps1
```

The editable SVG is `SleepGuard/Assets/SleepGuard.svg`. The generator draws the same
geometry with Windows GDI+ and embeds PNG frames into a multiresolution ICO.

## Validation

The source separates power profiles and data types, power-setting operations,
Win32 declarations and structures, tray behavior, and embedded icon loading.
`SleepGuard.Tests/FakePowerApi.cs` contains the fault-injecting backend used by the
power-setting tests. Native structures retain their Windows ABI field order.

Formatting follows `.editorconfig`; `stylecop.json` configures the existing StyleCop
analyzer. To check formatting and build the solution:

```powershell
dotnet format .\SleepGuard.slnx --no-restore --verify-no-changes --severity warn
dotnet build .\SleepGuard.slnx -c Release
```

Run the dependency-free tests:

```powershell
dotnet run --project .\SleepGuard.Tests\SleepGuard.Tests.csproj -c Release
```

Add `-- --read-windows` to also check the native read APIs. Tests use a fake power
backend for mutations and do not modify the computer's power settings.

The published executable also has a native smoke test:

```powershell
$process = Start-Process .\dist\SleepGuard\SleepGuard.exe -ArgumentList '--self-test' -WindowStyle Hidden -Wait -PassThru
$process.ExitCode
```

Exit code zero means the embedded icon loaded, the real tray and menu were created,
menu order/radio selection worked, unmanaged callbacks processed commands, a failed
Exit kept the app running, and a subsequent successful Exit restored Normal in the
test backend. This briefly displays a tray icon and never writes real power settings.

For manual acceptance on the target laptop:

1. Start SleepGuard and check that Normal is selected.
2. Open the menu with each mouse button and verify it dismisses on an outside click.
3. Select Awake and inspect the battery/AC values in Windows power settings.
4. Select Normal and verify that sleep and lid settings match the table.
5. Select Awake, then Exit; verify Normal settings and removal of the tray icon.

Read-only command-line checks for the plan settings:

```powershell
powercfg /getactivescheme
powercfg /query SCHEME_BALANCED SUB_SLEEP STANDBYIDLE
powercfg /query SCHEME_BALANCED SUB_BUTTONS LIDACTION
```

## References

- [Power mode API](https://learn.microsoft.com/en-us/windows/win32/api/powrprof/nf-powrprof-powersetuserconfiguredacpowermode)
- [Sleep timeout, measured in seconds](https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/sleep-settings-sleep-idle-timeout)
- [Lid action values](https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/power-button-and-lid-settings-lid-switch-close-action)
- [NativeAOT deployment and build prerequisites](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
