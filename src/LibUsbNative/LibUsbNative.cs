using System.Diagnostics;
using System.Runtime.InteropServices;
using LibUsbNative.SafeHandles;

namespace LibUsbNative;

#pragma warning disable IDE1006 // Naming Styles

/// <summary>
/// Singleton-style access to libusb API. Swap in tests if needed.
/// </summary>
public class LibUsbNative : ILibUsbNative
{
    private readonly ILibUsbApi _api;

    public LibUsbNative(ILibUsbApi? api = default)
    {
        _api = api ?? new PInvokeLibUsbApi();
    }

    public ISafeContext CreateContext()
    {
        return new SafeContext(_api);
    }

    public bool HasCapability(uint capability) => _api.libusb_has_capability((uint)capability) != 0;

    /// <summary>
    /// Native layout for libusb_version (libusb.h)
    /// struct libusb_version {
    ///   const uint16_t major, minor, micro, nano;
    ///   const char *rc;
    ///   const char *describe;
    /// };
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct native_libusb_version
    {
        public ushort major;
        public ushort minor;
        public ushort micro;
        public ushort nano;
        public IntPtr rc;
        public IntPtr describe;
    }

    /// <summary>
    /// Returns the full libusb version structure.
    /// </summary>
    public LibUsbVersion GetVersion()
    {
        var p = _api.libusb_get_version();
        if (p == IntPtr.Zero)
            throw new LibUsbException(LibUsbError.Other, "libusb_get_version returned null pointer");

        var native = Marshal.PtrToStructure<native_libusb_version>(p);

        static string PtrToString(IntPtr sp) =>
            sp == IntPtr.Zero ? string.Empty : (Marshal.PtrToStringAnsi(sp) ?? string.Empty);

        return new LibUsbVersion(
            native.major,
            native.minor,
            native.micro,
            native.nano,
            PtrToString(native.rc),
            PtrToString(native.describe)
        );
    }

    public string StrError(LibUsbError error)
    {
        var ptr = _api.libusb_strerror(error);
        Debug.Assert(ptr != IntPtr.Zero, "libusb_strerror returned null pointer");

        var detail = Marshal.PtrToStringAnsi(ptr);
        return detail is null ? $"LibUsb error code {error}." : $"LibUsb error code {error}: {detail}.";
    }
}

#pragma warning restore IDE1006 // Naming Styles
