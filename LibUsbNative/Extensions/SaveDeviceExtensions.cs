using System;
using System.Threading;
using LibUsbNative.SafeHandles;

namespace LibUsbNative.Extensions;

public static class SafeDeviceExtensions
{
    /*
        // -------- String descriptors --------
        public static string? GetStringAscii(this SafeDeviceHandle handle, byte index)
        {
            if (handle is null)
                throw new ArgumentNullException(nameof(handle));
            if (index == 0)
                return null;
            var buf = new byte[256];
            var rc = LibUsb.Api.libusb_get_string_descriptor_ascii(handle.DangerousGetHandle(), index, buf, buf.Length);
            if (rc <= 0)
                return null;
            return System.Text.Encoding.ASCII.GetString(buf, 0, (int)rc);
        }
    */
}
