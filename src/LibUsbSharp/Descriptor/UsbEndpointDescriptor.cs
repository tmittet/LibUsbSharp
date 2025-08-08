namespace LibUsbSharp.Descriptor;

/// <inheritdoc/>
public record struct UsbEndpointDescriptor(
    byte EndpointAddress,
    byte Attributes,
    ushort MaxPacketSize,
    byte Interval,
    byte Refresh,
    byte SynchAddress,
    byte[] ExtraBytes
) : IUsbEndpointDescriptor
{
    public readonly UsbEndpointDirection EndpointDirection =>
        EndpointAddress < 0x80 ? UsbEndpointDirection.Output : UsbEndpointDirection.Input;
}
