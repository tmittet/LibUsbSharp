using System.Diagnostics.CodeAnalysis;
using LibUsbNative.Enums;

namespace LibUsbNative.SafeHandles;

public interface ISafeDeviceHandle : IDisposable
{
    ISafeDevice Device { get; }

    bool IsClosed { get; }

    nint DangerousGetHandle();

    string GetStringDescriptorAscii(byte index);

    ISafeDeviceInterface ClaimInterface(int interfaceNumber);

    void ResetDevice();

    ISafeTransfer AllocateTransfer(int isoPackets = 0);
}
