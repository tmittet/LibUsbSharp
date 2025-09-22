namespace LibUsbNative;

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
