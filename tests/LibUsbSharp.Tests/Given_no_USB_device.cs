using FluentAssertions;
using LibUsbSharp.Tests.TestLogger;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace LibUsbSharp.Tests;

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

    [Fact]
    public void GetDeviceList_throws_when_called_without_Initialize()
    {
        using var libUsb = new LibUsb(_loggerFactory);
        var act = () => libUsb.GetDeviceList();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetDeviceList_throws_when_called_after_Dispose()
    {
        using var libUsb = new LibUsb(_loggerFactory);
        libUsb.Initialize(LogLevel.Information);
        libUsb.Dispose();
        var act = () => libUsb.GetDeviceList();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void RegisterHotplug_throws_when_called_without_Initialize_on_supported_platform()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return;
        }
        using var libUsb = new LibUsb(_loggerFactory);
        var act = () => libUsb.RegisterHotplug(vendorId: 0x2BD9);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RegisterHotplug_returns_true_when_called_after_Initialize_on_supported_platform()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return;
        }
        using var libUsb = new LibUsb(_loggerFactory);
        libUsb.Initialize(LogLevel.Information);
        var success = libUsb.RegisterHotplug(vendorId: 0x2BD9);
        success.Should().BeTrue();
    }

    [Fact]
    public void RegisterHotplug_returns_false_when_called_after_Initialize_on_unsupported_platform()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }
        using var libUsb = new LibUsb(_loggerFactory);
        libUsb.Initialize(LogLevel.Information);
        var success = libUsb.RegisterHotplug(vendorId: 0x2BD9);
        success.Should().BeFalse();
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
    }
}
