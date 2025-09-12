using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
