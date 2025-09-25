using System.Collections;
using System.Runtime.InteropServices;
using LibUsbNative.Enums;

namespace LibUsbNative.SafeHandles;

internal sealed class SafeDeviceList : SafeHandle, ISafeDeviceList
{
    private readonly int _count;
    private readonly Lazy<IReadOnlyList<SafeDevice>> _lazyDevices;
    private readonly SafeContext _context;

    public SafeDeviceList(SafeContext context, nint listPtr, int count)
        : base(listPtr, ownsHandle: true)
    {
        if (listPtr == IntPtr.Zero)
            throw new ArgumentNullException(nameof(listPtr));
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Must be greater than or equal to zero.");

        _context = context;
        _count = count;
        _lazyDevices = new Lazy<IReadOnlyList<SafeDevice>>(() => GetDevices(context, handle, count));
    }

    private static SafeDevice[] GetDevices(SafeContext context, nint handle, int count)
    {
        var devices = new SafeDevice[count];
        var ptrSize = IntPtr.Size;
        for (var i = 0; i < count; i++)
        {
            var devPtr = Marshal.ReadIntPtr(handle, i * ptrSize);

            var success = false;
            context.DangerousAddRef(ref success);
            if (!success)
            {
                throw LibUsbException.FromError(libusb_error.LIBUSB_ERROR_OTHER, "Failed to ref SafeHandle.");
            }

            devices[i] = new SafeDevice(context, devPtr);
        }
        return devices;
    }

    public ISafeDevice this[int index] => _lazyDevices.Value[index];

    public override bool IsInvalid => handle == IntPtr.Zero;

    public int Count
    {
        get
        {
            SafeHelpers.ThrowIfClosed(this);
            return _count;
        }
    }

    public IEnumerator<ISafeDevice> GetEnumerator()
    {
        SafeHelpers.ThrowIfClosed(this);
        return _lazyDevices.Value.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        SafeHelpers.ThrowIfClosed(this);
        return _lazyDevices.Value.GetEnumerator();
    }

    protected override bool ReleaseHandle()
    {
        if (IsInvalid)
            return true;

        if (_lazyDevices.IsValueCreated)
        {
            foreach (var device in _lazyDevices.Value)
            {
                if (!device.IsClosed)
                {
                    device.Dispose();
                }
            }
        }

        _context.api.libusb_free_device_list(handle, unrefDevices: 1);
        _context.DangerousRelease();
        return true;
    }
}
