using LibUsbSharp.Internal.Descriptor;

namespace LibUsbSharp.Descriptor;

/// <inheritdoc/>
public readonly struct UsbDeviceDescriptor : IUsbDeviceDescriptor
{
    /// <inheritdoc/>
    public string DeviceKey { get; init; }

    /// <inheritdoc/>
    public ushort BcdUsb { get; init; }

    /// <inheritdoc/>
    public UsbClass DeviceClass { get; init; }

    /// <inheritdoc/>
    public byte DeviceSubClass { get; init; }

    /// <inheritdoc/>
    public byte DeviceProtocol { get; init; }

    /// <inheritdoc/>
    public byte MaxPacketSize0 { get; init; }

    /// <inheritdoc/>
    public ushort VendorId { get; init; }

    /// <inheritdoc/>
    public ushort ProductId { get; init; }

    /// <inheritdoc/>
    public ushort BcdDevice { get; init; }

    /// <inheritdoc/>
    public byte ManufacturerIndex { get; init; }

    /// <inheritdoc/>
    public byte ProductIndex { get; init; }

    /// <inheritdoc/>
    public byte SerialNumberIndex { get; init; }

    /// <inheritdoc/>
    public byte NumConfigurations { get; init; }

    /// <inheritdoc/>
    public byte BusNumber { get; init; }

    /// <inheritdoc/>
    public byte BusAddress { get; init; }

    /// <inheritdoc/>
    public byte PortNumber { get; init; }

    /// <summary>
    /// Create a string device key.
    /// </summary>
    public static string GetKey(ushort vendorId, ushort productId, byte busNumber, byte busAddress)
    {
        return $"{vendorId:X4}_{productId:X4}_{busNumber}_{busAddress}";
    }

    internal UsbDeviceDescriptor(
        LibUsbNative.Descriptors.IUsbDeviceDescriptor partialDescriptor,
        byte busNumber,
        byte address,
        byte portNumber
    )
    {
        BcdUsb = partialDescriptor.BcdUSB;
        DeviceClass = (UsbClass)partialDescriptor.BDeviceClass;
        DeviceSubClass = partialDescriptor.BDeviceSubClass;
        DeviceProtocol = partialDescriptor.BDeviceProtocol;
        MaxPacketSize0 = partialDescriptor.BMaxPacketSize0;
        VendorId = partialDescriptor.IdVendor;
        ProductId = partialDescriptor.IdProduct;
        BcdDevice = partialDescriptor.BcdDevice;
        ManufacturerIndex = partialDescriptor.IManufacturer;
        ProductIndex = partialDescriptor.IProduct;
        SerialNumberIndex = partialDescriptor.ISerialNumber;
        NumConfigurations = partialDescriptor.BNumConfigurations;
        BusNumber = busNumber;
        BusAddress = address;
        PortNumber = portNumber;

        DeviceKey = GetKey(VendorId, ProductId, BusNumber, BusAddress);
    }

    public override string ToString() => DeviceKey;
}
