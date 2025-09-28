using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibUsbSharp.Native.SafeHandles;

internal static class SafeHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfClosed(SafeHandle safeHandle, string? objectName = "SafeHandle")
    {
        if (safeHandle.IsClosed || safeHandle.IsInvalid)
        {
            throw new ObjectDisposedException(objectName);
        }
    }
}
