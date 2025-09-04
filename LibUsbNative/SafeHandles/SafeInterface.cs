using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibUsbNative.SafeHandles;

public interface ISafeDeviceInterface : IDisposable
{
    int GetInterfaceNumber();
}

internal sealed class SafeDeviceInterface : SafeHandle, ISafeDeviceInterface
{
    private readonly SafeDeviceHandle _deviceHandle;
    private readonly int _interfaceNumber;

    public SafeDeviceInterface(SafeDeviceHandle deviceHandle, int interfaceNumber)
        : base(IntPtr.Zero, true)
    {
        _deviceHandle = deviceHandle;
        _interfaceNumber = interfaceNumber;
    }

    public int GetInterfaceNumber() => _interfaceNumber;

    public override bool IsInvalid => false;

    protected override bool ReleaseHandle()
    {
        var result = LibUsb.Api.libusb_release_interface(_deviceHandle.DangerousGetHandle(), _interfaceNumber);
        LibUsbException.ThrowIfError(result, $"Failed to release interface {_interfaceNumber}.");
        return true;
    }
}
