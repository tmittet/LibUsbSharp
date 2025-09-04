using System.Diagnostics;
using System.Runtime.InteropServices;
using LibUsbNative;
using LibUsbNative.SafeHandles;

namespace LibUsbNative;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CA1707 // Identifiers should not contain underscores

/// <summary>
/// Singleton-style access to libusb API. Swap in tests if needed.
/// </summary>
public static class LibUsb
{
    internal static ILibUsbApi Api { get; set; } = new PInvokeLibUsbApi();

    public static ISafeContext CreateContext()
    {
        return new SafeContext();
    }

    public static bool HasCapability(uint capability) => Api.libusb_has_capability((uint)capability) != 0;

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
    /// Managed projection of libusb_version.
    /// </summary>
    public readonly record struct LibUsbVersion(
        ushort Major,
        ushort Minor,
        ushort Micro,
        ushort Nano,
        string Rc,
        string Describe
    )
    {
        public override string ToString()
        {
            var baseVer = $"{Major}.{Minor}.{Micro}.{Nano}";
            var rcPart = string.IsNullOrWhiteSpace(Rc) ? "" : $" ({Rc})";
            var descPart = string.IsNullOrWhiteSpace(Describe) ? "" : $" - {Describe}";
            return $"libusb {baseVer}{rcPart}{descPart}";
        }
    }

    /// <summary>
    /// Returns the full libusb version structure.
    /// </summary>
    public static LibUsbVersion GetVersion()
    {
        var p = Api.libusb_get_version();
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

    public static string StrError(LibUsbError error)
    {
        var ptr = LibUsb.Api.libusb_strerror(error);
        Debug.Assert(ptr != IntPtr.Zero, "libusb_strerror returned null pointer");

        var detail = Marshal.PtrToStringAnsi(ptr);
        return detail is null ? $"LibUsb error code {error}." : $"LibUsb error code {error}: {detail}.";
    }
}

#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore CA1707 // Identifiers should not contain underscores
#pragma warning restore IDE0079 // Remove unnecessary suppression
