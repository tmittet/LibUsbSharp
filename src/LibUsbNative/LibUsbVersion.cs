namespace LibUsbNative;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CA1707 // Identifiers should not contain underscores

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

#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore CA1707 // Identifiers should not contain underscores
#pragma warning restore IDE0079 // Remove unnecessary suppression
