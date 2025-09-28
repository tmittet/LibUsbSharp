namespace LibUsbSharp.Native.SafeHandles;

public interface ISafeConfigDescriptorPtr : IDisposable
{
    nint GetUnmanagedPointer();
}
