namespace LibUsbNative.Structs;

/// <summary>
/// Structure providing the version of the libusb runtime.
/// </summary>
public readonly record struct libusb_version(
    ushort major,
    ushort minor,
    ushort micro,
    ushort nano,
    string rc,
    string describe
)
{
    public override string ToString()
    {
        var baseVer = $"{major}.{minor}.{micro}.{nano}";
        var rcPart = string.IsNullOrWhiteSpace(rc) ? "" : $" ({rc})";
        var descPart = string.IsNullOrWhiteSpace(describe) ? "" : $" - {describe}";
        return $"libusb {baseVer}{rcPart}{descPart}";
    }
}
