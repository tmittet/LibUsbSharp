using LibUsbNative.Enums;

namespace LibUsbNative.SafeHandles;

public interface ISafeTransfer : IDisposable
{
    LibUsbError Submit();
    LibUsbError Cancel();
    IntPtr GetBufferPtr();
}
