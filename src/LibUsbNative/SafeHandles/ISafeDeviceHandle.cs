using LibUsbNative.Enums;

namespace LibUsbNative.SafeHandles;

public interface ISafeDeviceHandle : IDisposable
{
    ISafeDevice Device { get; }
    bool IsClosed { get; }

    IntPtr DangerousGetHandle();
    string GetStringDescriptorAscii(byte index);
    ISafeDeviceInterface ClaimInterface(int interfaceNumber);
    libusb_error ResetDevice();
    ISafeTransfer AllocateTransfer(int isoPackets = 0);
}
