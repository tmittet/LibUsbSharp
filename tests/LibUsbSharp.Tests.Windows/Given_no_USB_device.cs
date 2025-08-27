namespace LibUsbSharp.Tests.Windows;

public sealed class Given_no_USB_device : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;

    public Given_no_USB_device(ITestOutputHelper output)
    {
        _loggerFactory = new TestLoggerFactory(output);
    }

    [Fact]
    public void GetVersion_returns_a_valid_version_of_at_least_1_0_27()
    {
        var version = LibUsb.GetVersion();
        // Log callback requires v1.0.27 or above
        version.Should().BeGreaterThanOrEqualTo(new Version(1, 0, 27));
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
    }
}
