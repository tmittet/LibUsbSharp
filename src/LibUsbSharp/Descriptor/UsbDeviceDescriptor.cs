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

    /// <summary>
    /// The index of the manufacturer string descriptor.
    /// </summary>
    internal byte ManufacturerIndex { get; init; }

    /// <summary>
    /// The index of the product string descriptor.
    /// </summary>
    internal byte ProductIndex { get; init; }

    /// <summary>
    /// The index of the device serial number string descriptor.
    /// </summary>
    internal byte SerialNumberIndex { get; init; }

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
        LibUsbDeviceDescriptor partialDescriptor,
        byte busNumber,
        byte address,
        byte portNumber
    )
    {
        BcdUsb = partialDescriptor.BcdUsb;
        DeviceClass = partialDescriptor.DeviceClass;
        DeviceSubClass = partialDescriptor.DeviceSubClass;
        DeviceProtocol = partialDescriptor.DeviceProtocol;
        MaxPacketSize0 = partialDescriptor.MaxPacketSize0;
        VendorId = partialDescriptor.VendorId;
        ProductId = partialDescriptor.ProductId;
        BcdDevice = partialDescriptor.BcdDevice;
        ManufacturerIndex = partialDescriptor.Manufacturer;
        ProductIndex = partialDescriptor.Product;
        SerialNumberIndex = partialDescriptor.SerialNumber;
        NumConfigurations = partialDescriptor.NumConfigurations;
        BusNumber = busNumber;
        BusAddress = address;
        PortNumber = portNumber;

        DeviceKey = GetKey(VendorId, ProductId, BusNumber, BusAddress);
    }

    public override string ToString() => DeviceKey;
}
