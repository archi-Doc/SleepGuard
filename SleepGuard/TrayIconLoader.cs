// SleepGuard - Windows power profile tray utility.

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using static SleepGuard.NativeConstants;
using static SleepGuard.NativeMethods;

namespace SleepGuard;

internal static unsafe class TrayIconLoader
{
    private const int DirectoryHeaderSize = 6;
    private const int DirectoryEntrySize = 16;

    internal static nint Load()
    {
        using var stream = typeof(TrayIconLoader).Assembly.GetManifestResourceStream("SleepGuard.Icon")
            ?? throw new InvalidOperationException("The SleepGuard icon resource is missing.");
        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        int count = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(4));
        var target = Math.Max(16, GetSystemMetrics(SmallIconWidth));
        var best = DirectoryHeaderSize;
        var distance = int.MaxValue;
        for (var i = 0; i < count; i++)
        {
            var entry = DirectoryHeaderSize + (i * DirectoryEntrySize);
            var size = bytes[entry] == 0 ? 256 : bytes[entry];
            var score = Math.Abs(size - target);
            if (score < distance)
            {
                best = entry;
                distance = score;
            }
        }

        var length = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(best + 8));
        var offset = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(best + 12)));
        fixed (byte* data = &bytes[offset])
        {
            var result = CreateIconFromResourceEx(data, length, 1, IconResourceVersion, target, target, 0);
            if (result == 0)
            {
                throw new InvalidOperationException($"Load the SleepGuard icon failed (Windows error {Marshal.GetLastPInvokeError()}).");
            }

            return result;
        }
    }
}
