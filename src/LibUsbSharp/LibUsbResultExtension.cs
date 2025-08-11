using System.Runtime.InteropServices;

namespace LibUsbSharp;

public static class LibUsbResultExtension
{
    public static string GetMessage(this LibUsbResult result)
    {
        var errorCode = (int)result;
        var ptr = libusb_strerror(errorCode);
        var detail = Marshal.PtrToStringAnsi(ptr);
        return detail is null
            ? $"LibUsb error code {errorCode}: {result}."
            : $"LibUsb error code {errorCode}: {detail}.";
    }

#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute'

    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern nint libusb_strerror(int errorCode);

#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute'
}
