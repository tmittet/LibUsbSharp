namespace LibUsbNative;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CA1707 // Identifiers should not contain underscores

public static class UsbRequestType
{
    // bmRequestType (USB 2.0 spec)
    public const byte DirectionIn = 0x80;
    public const byte DirectionOut = 0x00;

    public const byte TypeStandard = 0x00;
    public const byte TypeClass = 0x20;
    public const byte TypeVendor = 0x40;
    public const byte TypeReserved = 0x60;

    public const byte RecipDevice = 0x00;
    public const byte RecipInterface = 0x01;
    public const byte RecipEndpoint = 0x02;
    public const byte RecipOther = 0x03;
}

public enum LibusbSpeed
{
    Unknown = 0,
    Low = 1,
    Full = 2,
    High = 3,
    Super = 4,
    SuperPlus = 5,
}

public static class Hotplug
{
    public const int LIBUSB_HOTPLUG_EVENT_DEVICE_ARRIVED = 0x01;
    public const int LIBUSB_HOTPLUG_EVENT_DEVICE_LEFT = 0x02;
    public const int LIBUSB_HOTPLUG_ENUMERATE = 0x01;
    public const int LIBUSB_HOTPLUG_NO_FLAGS = 0x00;
    public const int LIBUSB_HOTPLUG_MATCH_ANY = -1;
}

#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore CA1707 // Identifiers should not contain underscores
#pragma warning restore IDE0079 // Remove unnecessary suppression
