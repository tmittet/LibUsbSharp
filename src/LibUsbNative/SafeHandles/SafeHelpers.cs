using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibUsbNative.SafeHandles;

internal static class SafeHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfClosed(SafeHandle safeHandle, string? objectName = "SafeHandle")
    {
        if (safeHandle.IsClosed || safeHandle.IsInvalid)
        {
            ThrowObjectDisposedException(objectName);
        }
    }

    [DoesNotReturn]
    private static void ThrowObjectDisposedException(string? objectName)
    {
        throw new ObjectDisposedException(objectName);
    }
}
