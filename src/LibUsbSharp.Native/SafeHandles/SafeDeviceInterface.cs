using System.Runtime.InteropServices;

namespace LibUsbSharp.Native.SafeHandles;

internal sealed class SafeDeviceInterface : SafeHandle, ISafeDeviceInterface
{
    private readonly SafeDeviceHandle _deviceHandle;
    private readonly byte _interfaceNumber;

    public SafeDeviceInterface(SafeDeviceHandle deviceHandle, byte interfaceNumber)
        : base(IntPtr.Zero, true)
    {
        _deviceHandle = deviceHandle;
        _interfaceNumber = interfaceNumber;
    }

    public int GetInterfaceNumber()
    {
        SafeHelpers.ThrowIfClosed(this);
        return _interfaceNumber;
    }

    public override bool IsInvalid => false;

    protected override bool ReleaseHandle()
    {
        var result = _deviceHandle._context.api.libusb_release_interface(
            _deviceHandle.DangerousGetHandle(),
            _interfaceNumber
        );
        LibUsbException.ThrowIfApiError(
            result,
            nameof(_deviceHandle._context.api.libusb_release_interface),
            $"Interface {_interfaceNumber}."
        );
        return true;
    }
}
