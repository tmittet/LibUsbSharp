namespace LibUsbSharp.Native.SafeHandles;

public interface ISafeConfigDescriptor : IDisposable
{
    nint GetUnmanagedPointer();

    /// <summary>
    /// Gets a value indicating whether the underlying handle is closed or not.
    /// NOTE: Even though the safe type is disposed, the handle may remain open.
    /// </summary>
    bool IsClosed { get; }
}
