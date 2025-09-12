using LibUsbNative;
using LibUsbNative.Descriptor;
using LibUsbNative.SafeHandles;

// See https://aka.ms/new-console-template for more information

Console.WriteLine($"LibUsb version: {LibUsbNative.LibUsbNative.GetVersion()}");

var context = LibUsbNative.LibUsbNative.CreateContext();
context.RegisterLogCallback(
    (level, message) =>
    {
        Console.WriteLine($"[LibUsb][{level}] {message}");
    }
);

var (deviceList, count) = context.GetDeviceList();
Console.WriteLine($"Found {count} USB devices.");

using (deviceList)
{
    foreach (var device in deviceList.Devices)
    {
        IUsbDeviceDescriptor desc = device.GetDeviceDescriptor();
        Console.WriteLine(desc.ToJson());
    }
}
