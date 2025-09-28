namespace LibUsbSharp.Native.Enums;

/// <summary>
/// Usage type for isochronous endpoints.
/// Values for bits 4:5 of the bmAttributes field in libusb_endpoint_descriptor.
/// </summary>
public enum libusb_iso_usage_type : byte
{
    /// <summary>Data endpoint.</summary>
    LIBUSB_ISO_USAGE_TYPE_DATA = 0,

    /// <summary>Feedback endpoint.</summary>
    LIBUSB_ISO_USAGE_TYPE_FEEDBACK = 1,

    /// <summary>Implicit feedback Data endpoint.</summary>
    LIBUSB_ISO_USAGE_TYPE_IMPLICIT = 2,
}
