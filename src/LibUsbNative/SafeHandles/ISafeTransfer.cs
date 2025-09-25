using LibUsbNative.Enums;

namespace LibUsbNative.SafeHandles;

public interface ISafeTransfer : IDisposable
{
    libusb_error Submit();

    libusb_error Cancel();

    nint GetBufferPtr();
}
