using LibUsbSharp.Internal.Transfer;

namespace LibUsbSharp.Tests.Internal;

public class LibUsbControlRequestSetupTest
{
    [Theory]
    [InlineData(ControlRequestRecipient.Device, ControlRequestType.Standard, 0b10000000)]
    [InlineData(ControlRequestRecipient.Interface, ControlRequestType.Standard, 0b10000001)]
    [InlineData(ControlRequestRecipient.Endpoint, ControlRequestType.Standard, 0b10000010)]
    [InlineData(ControlRequestRecipient.Endpoint, ControlRequestType.Class, 0b10100010)]
    [InlineData(ControlRequestRecipient.Endpoint, ControlRequestType.Vendor, 0b11000010)]
    public void Read_returns_expected_setup_packet_request_byte(
        ControlRequestRecipient recipient,
        ControlRequestType type,
        byte expectedValue
    )
    {
        var setup = LibUsbControlRequestSetup.Read(recipient, type, 0, 0, 0, 0);
        var buffer = setup.CreateBuffer();
        buffer[0].Should().Be(expectedValue, Convert.ToString(buffer[0], 2).PadLeft(8, '0'));
    }

    [Theory]
    [InlineData(ControlRequestRecipient.Device, ControlRequestType.Standard, 0b00000000)]
    [InlineData(ControlRequestRecipient.Interface, ControlRequestType.Standard, 0b00000001)]
    [InlineData(ControlRequestRecipient.Endpoint, ControlRequestType.Standard, 0b00000010)]
    [InlineData(ControlRequestRecipient.Endpoint, ControlRequestType.Class, 0b00100010)]
    [InlineData(ControlRequestRecipient.Endpoint, ControlRequestType.Vendor, 0b01000010)]
    public void Write_returns_expected_setup_packet_request_byte(
        ControlRequestRecipient recipient,
        ControlRequestType type,
        byte expectedValue
    )
    {
        var setup = LibUsbControlRequestSetup.Write(recipient, type, 0, 0, 0, 0);
        var buffer = setup.CreateBuffer();
        buffer[0].Should().Be(expectedValue);
    }
}
