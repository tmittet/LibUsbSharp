using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace LibUsbSharp.Tests;

public sealed class Given_a_specific_Huddly_USB_device : IDisposable
{
    private const ushort HuddlyVendorId = 0x2BD9;
    private const ushort HuddlyProductId = 0x0021;
    private const string HuddlySerial = "B43K00542";

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<Given_a_specific_Huddly_USB_device> _logger;

    public void Dispose()
    {
        _loggerFactory.Dispose();
    }

    public Given_a_specific_Huddly_USB_device(ITestOutputHelper output)
    {
        _loggerFactory = new TestLoggerFactory(output);
        _logger = _loggerFactory.CreateLogger<Given_a_specific_Huddly_USB_device>();
    }

    /*
     *
     * TODO: Test that operations fail as expected when no admin
   [Fact]
    public void SerializedIntegrationTestsNoAdmin()
    {

    */

    [Fact(Skip = "WIP")]
    public void SerializedIntegrationTests()
    {
        if (OperatingSystem.IsWindows())
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);

            bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

            if (isAdmin)
            {
                _logger.LogInformation("You are running as Administrator.");
            }
            else
            {
                _logger.LogError("You are NOT running as Administrator.");
            }
        }

        using var _libUsb = new LibUsb(_loggerFactory);
        _libUsb.Initialize(LogLevel.Warning);

        // Should this fail?
        using var _libUsb2 = new LibUsb(_loggerFactory);

        GC.Collect();
        var handle_count = Handles.GetHandleCount();
        _logger.LogInformation("Handle count: {HandleCount}", handle_count);

        var descriptors = _libUsb.GetDeviceList(
            vendorId: HuddlyVendorId,
            productId: HuddlyProductId
        );
        Assert.True(descriptors.Count == 1, "No USB device available.");

        var descriptor = descriptors[0];

        Assert.True(descriptor.VendorId == HuddlyVendorId, "Unexpected Vendor ID.");
        Assert.True(descriptor.ProductId == HuddlyProductId, "Unexpected Product ID.");

        _logger.LogInformation(
            "Device found: Class={DeviceClass}, VID=0x{VID:X4}, PID=0x{PID:X4}, "
                + "BusNumber={BusNumber}, BusAddress={BusAddress}, PortNumber={PortNumber}.",
            descriptor.DeviceClass,
            descriptor.VendorId,
            descriptor.ProductId,
            descriptor.BusNumber,
            descriptor.BusAddress,
            descriptor.PortNumber
        );

        var serial = _libUsb.GetDeviceSerial(descriptor);

        GC.Collect();
        handle_count = Handles.GetHandleCount();
        _logger.LogInformation("Handle count: {HandleCount}", handle_count);

        _logger.LogInformation("Device serial: {Serial}", serial);
        Assert.True(serial == HuddlySerial, "Unexpected serial number.");

        var tasks = new List<Task>();
        int numThreads = 10;

        for (int i = 0; i < numThreads; i++)
        {
            tasks.Add(
                Task.Run(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var serial = _libUsb.GetDeviceSerial(descriptor);
                        Assert.True(serial == HuddlySerial, "Unexpected serial number.");
                    }
                })
            );
        }

#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Task.WaitAll(tasks.ToArray());
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

        _libUsb.Dispose();

        GC.Collect();
        handle_count = Handles.GetHandleCount();
        _logger.LogInformation("Handle count: {HandleCount}", handle_count);

        Assert.Throws<ObjectDisposedException>(() =>
        {
            _ = _libUsb.GetDeviceSerial(descriptor);
        });

        //var device = _libUsb.OpenDevice(descriptor);

        GC.Collect();
        handle_count = Handles.GetHandleCount();
        _logger.LogInformation("Handle count: {HandleCount}", handle_count);

        //        var device = _libUsb.OpenDevice(descriptor);
    }
}
