namespace LibUsbSharp.Native.SafeHandles;

public interface ISafeConfigDescriptor : IDisposable
{
    nint GetUnmanagedPointer();
}
