using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace LibUsbSharp.Internal;

/// <summary>
/// LibUsb does not start any threads of its own. Async operations are driven by this event loop.
/// See: https://libusb.sourceforge.io/api-1.0/group__libusb__asyncio.html#asyncevent for info.
/// </summary>
internal sealed class LibUsbEventLoop : IDisposable
{
    private readonly object _lock = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<LibUsbEventLoop> _logger;
    private readonly IntPtr _context;
    private readonly CancellationTokenSource _cts;
    private readonly IntPtr _completedPtr;
    private Thread? _thread;
    private bool _disposed;

    public LibUsbEventLoop(ILoggerFactory loggerFactory, nint context)
    {
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<LibUsbEventLoop>();
        _context = context;
        _cts = new CancellationTokenSource();
        _completedPtr = Marshal.AllocHGlobal(sizeof(int));
        Marshal.WriteInt32(_completedPtr, 0);
    }

    /// <summary>
    /// Start the background thread that handles LibUsb events. All LibUsb event handling is
    /// performed this thread. LibUsb does not invoke any callbacks outside of this context.
    /// Consequently, all registered callbacks will be run on this thread.
    /// See: https://libusb.sourceforge.io/api-1.0/group__libusb__asyncio.html#eventthread
    /// </summary>
    public void Start()
    {
        lock (_lock)
        {
            CheckDisposed();
            if (_thread is not null)
            {
                throw new InvalidOperationException($"LibUsbEventLoop already started.");
            }
            _thread = new Thread(() => HandleEventsLoop(_cts.Token)) { IsBackground = true };
            _thread.Start();
        }
    }

    private void HandleEventsLoop(CancellationToken token)
    {
        try
        {
            _logger.LogDebug("HandleEventsLoop started.");
            while (!token.IsCancellationRequested)
            {
                // libusb does not write to completed, so there is no reason to check it
                // See: https://github.com/libusb/libusb/blob/master/libusb/io.c
                var result = libusb_handle_events_completed(_context, _completedPtr);
                // libusb_handle_events can return LibUsbResult.Interrupted transiently;
                // do not exit the loop on LibUsbResult.Interrupted.
                if (result != 0 && result != (int)LibUsbResult.Interrupted)
                {
                    _logger.LogWarning(
                        "LibUsb HandleEvents failed; exiting event loop. {ErrorDetail}",
                        ((LibUsbResult)result).GetMessage()
                    );
                    break;
                }
#if DEBUG
                var completed = Marshal.ReadInt32(_completedPtr) != 0;
                _logger.LogTrace("libusb_handle_events_completed '{Completed}'.", completed);
#endif
            }
            _logger.LogDebug("HandleEventsLoop stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "libusb_handle_events_completed error.");
        }
    }

    /// <summary>
    /// Throw ObjectDisposedException when LibUsb is disposed.
    /// </summary>
    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LibUsbEventLoop));
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            if (_thread is not null)
            {
                // Signal cancellation here to stop the HandleEventsLoop
                _cts.Cancel();
                // Set completed = 1 so libusb_handle_events_completed exits if currently blocking
                Marshal.WriteInt32(_completedPtr, 1);
                // Call libusb_interrupt_event_handler to unblock event handler and allow exit.
                // This is required since we don't follow the exact pattern recommended by libusb.
                // See: https://libusb.sourceforge.io/api-1.0/group__libusb__asyncio.html#eventthread
                // and: https://libusb.sourceforge.io/api-1.0/group__libusb__poll.html#ga188b6c50944b49f122ccfd45b93fa9f2
                // We deregister hotplug events, which wakes up libusb_handle_events, first then
                // stop the event loop; hence the event handler would block forever.
                _ = libusb_interrupt_event_handler(_context);
                // Wait for libusb_handle_events_completed and the HandleEventsLoop to stop
                _thread.Join();
            }
            Marshal.FreeHGlobal(_completedPtr);
            _cts.Dispose();
        }
    }

    // LibraryImportAttribute not available in .NET6, silence warning
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute'

    /// <summary>
    /// Handle any pending events in blocking mode. Like libusb_handle_events(), With a completed
    /// parameter to allow for race free waiting for the completion of a specific transfer.
    /// </summary>
    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe int libusb_handle_events_completed(
        IntPtr context,
        IntPtr completed
    );

    /// <summary>
    /// Interrupt any active thread that is handling events. This is mainly useful for interrupting
    /// a dedicated event handling thread when an application wishes to call libusb_exit().
    /// </summary>
    [DllImport(LibUsb.LibraryName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int libusb_interrupt_event_handler(IntPtr context);

#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute'
}
