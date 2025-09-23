namespace LibUsbNative.SafeHandles;

public interface ISafeConfigDescriptorPtr : IDisposable
{
    IntPtr GetUnmanagedPointer();
}
