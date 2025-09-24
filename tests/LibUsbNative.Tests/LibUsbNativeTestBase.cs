using LibUsbNative.Enums;
using LibUsbNative.Extensions;
using LibUsbNative.SafeHandles;

namespace LibUsbNative.Tests;

public class LibUsbNativeTestBase(ITestOutputHelper _output, ILibUsbApi _api)
{
    private static readonly ReaderWriterLockSlim rw_lock = new();

    private readonly LibUsbNative _libUsb = new(_api);

    protected ITestOutputHelper Output { get; } = _output;
    protected List<string> LibUsbOutput { get; } = [];

    protected ISafeContext GetContext()
    {
        var version = _libUsb.GetVersion();
        Output.WriteLine(version.ToString());

        var context = _libUsb.CreateContext();
        context.RegisterLogCallback(
            (level, message) =>
            {
                Output.WriteLine($"[Libusb][{level}] {message}");
                LibUsbOutput.Add(message);
            }
        );

        context.SetOption(libusb_log_level.LIBUSB_LOG_LEVEL_INFO);
        return context;
    }

    protected static void EnterReadLock(Action action)
    {
        rw_lock.EnterReadLock();
        try
        {
            action();
        }
        finally
        {
            rw_lock.ExitReadLock();
        }
    }

    protected static void EnterWriteLock(Action action)
    {
        rw_lock.EnterReadLock();
        try
        {
            action();
        }
        finally
        {
            rw_lock.ExitReadLock();
        }
    }
}
