namespace LibUsbSharp.Native.SafeHandles;

public interface ISafeDeviceInterface : IDisposable
{
    /// <summary>
    /// Get the interface number.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the ISafeDeviceInterface is disposed.</exception>
    int GetInterfaceNumber();
}
