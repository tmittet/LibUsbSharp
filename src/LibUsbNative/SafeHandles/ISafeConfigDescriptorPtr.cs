namespace LibUsbNative.SafeHandles;

public interface ISafeConfigDescriptorPtr : IDisposable
{
    nint GetUnmanagedPointer();
}
