// SleepGuard - Windows power profile tray utility.

using System.Runtime.InteropServices;

namespace SleepGuard;

internal sealed partial class WindowsPowerApi
{
    private static partial class Native
    {
        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerGetActiveScheme(nint root, out nint scheme);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerSetActiveScheme(nint root, in Guid scheme);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerGetUserConfiguredACPowerMode(out Guid mode);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerGetUserConfiguredDCPowerMode(out Guid mode);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerSetUserConfiguredACPowerMode(in Guid mode);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerSetUserConfiguredDCPowerMode(in Guid mode);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerReadACValueIndex(
            nint root,
            in Guid scheme,
            in Guid group,
            in Guid setting,
            out uint value);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerReadDCValueIndex(
            nint root,
            in Guid scheme,
            in Guid group,
            in Guid setting,
            out uint value);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerWriteACValueIndex(
            nint root,
            in Guid scheme,
            in Guid group,
            in Guid setting,
            uint value);

        [LibraryImport("powrprof.dll")]
        internal static partial uint PowerWriteDCValueIndex(
            nint root,
            in Guid scheme,
            in Guid group,
            in Guid setting,
            uint value);

        [LibraryImport("kernel32.dll")]
        internal static partial nint LocalFree(nint memory);
    }
}
