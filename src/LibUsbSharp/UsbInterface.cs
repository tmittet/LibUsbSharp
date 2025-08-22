using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using LibUsbSharp.Descriptor;
using LibUsbSharp.Internal.Transfer;
using Microsoft.Extensions.Logging;

namespace LibUsbSharp;

public sealed class UsbInterface : IUsbInterface
{
    // These buffers should be a multiple of the USB endpoint MaxPacketSize.
    // Typical MaxPacketSize values for USB 2.0 and 3.0 are 512 and 1024.
    private const int ReadBufferSize = 32 * 1024;
    private const int WriteBufferSize = 32 * 1024;

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<UsbInterface> _logger;
    private readonly UsbDevice _device;
    private readonly IUsbInterfaceDescriptor _descriptor;
    private readonly byte[] _bulkReadBuffer;
    private readonly GCHandle _bulkReadBufferHandle;
    private readonly Lazy<IUsbEndpointDescriptor> _readEndpoint;
    private readonly object _bulkReadLock = new();
    private readonly byte[] _bulkWriteBuffer;
    private readonly GCHandle _bulkWriteBufferHandle;
    private readonly Lazy<IUsbEndpointDescriptor> _writeEndpoint;
    private readonly object _bulkWriteLock = new();
    private readonly ReaderWriterLockSlim _disposeLock = new();
    private readonly CancellationTokenSource _disposeCts;
    private volatile bool _disposed;

    /// <inheritdoc />
    public byte Number => _descriptor.InterfaceNumber;

    /// <summary>
    /// A type representing a claimed USB interface.
    /// </summary>
    /// <param name="loggerFactory">An optional logger factory.</param>
    /// <param name="device">The parent USB device.</param>
    /// <param name="descriptor">The USB interface descriptor.</param>
    /// <param name="readEndpoint">
    /// Optional read endpoint. When nothing is specified and a read operation is attempted,
    /// an attempt is made to pick the first available "input" endpoint for this interface.
    /// </param>
    /// <param name="writeEndpoint">
    /// Optional write endpoint. When nothing is specified and a write operation is attempted,
    /// an attempt is made to pick the first available "output" endpoint for this interface.
    /// </param>
    public UsbInterface(
        ILoggerFactory loggerFactory,
        UsbDevice device,
        IUsbInterfaceDescriptor descriptor,
        IUsbEndpointDescriptor? readEndpoint = default,
        IUsbEndpointDescriptor? writeEndpoint = default
    )
    {
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<UsbInterface>();
        _device = device;
        _descriptor = descriptor;
        _bulkReadBuffer = new byte[ReadBufferSize];
        _bulkReadBufferHandle = GCHandle.Alloc(_bulkReadBuffer, GCHandleType.Pinned);
        _readEndpoint = readEndpoint is null
            ? new Lazy<IUsbEndpointDescriptor>(() =>
                GetEndpoint(descriptor, UsbEndpointDirection.Input)
            )
            : new Lazy<IUsbEndpointDescriptor>(readEndpoint);
        _bulkWriteBuffer = new byte[WriteBufferSize];
        _bulkWriteBufferHandle = GCHandle.Alloc(_bulkWriteBuffer, GCHandleType.Pinned);
        _writeEndpoint = writeEndpoint is null
            ? new Lazy<IUsbEndpointDescriptor>(() =>
                GetEndpoint(descriptor, UsbEndpointDirection.Output)
            )
            : new Lazy<IUsbEndpointDescriptor>(writeEndpoint);
        _disposeCts = new CancellationTokenSource();
    }

    /// <inheritdoc />
    public bool TryGetInputEndpoint([NotNullWhen(true)] out IUsbEndpointDescriptor? endpoint)
    {
        try
        {
            endpoint = _readEndpoint.Value;
            return true;
        }
        catch (InvalidOperationException)
        {
            endpoint = null;
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryGetOutputEndpoint([NotNullWhen(true)] out IUsbEndpointDescriptor? endpoint)
    {
        try
        {
            endpoint = _writeEndpoint.Value;
            return true;
        }
        catch (InvalidOperationException)
        {
            endpoint = null;
            return false;
        }
    }

    public LibUsbResult BulkRead(byte[] destination, out int bytesRead, int timeout) =>
        BulkRead(destination.AsSpan(), out bytesRead, timeout);

    /// <inheritdoc />
    public LibUsbResult BulkRead(Span<byte> destination, out int bytesRead, int timeout)
    {
        CheckTransferTimeout(timeout);
        _disposeLock.EnterReadLock(); // Use read lock for reads and writes, to support duplex
        try
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UsbInterface));
            }
            var bufferLength = Math.Min(destination.Length, ReadBufferSize);
            lock (_bulkReadLock)
            {
                var result = LibUsbTransfer.ExecuteSync(
                    _logger,
                    _device.Handle,
                    LibUsbTransferType.Bulk,
                    _readEndpoint.Value.EndpointAddress,
                    _bulkReadBufferHandle,
                    bufferLength,
                    timeout > 0 ? (uint)timeout : 0,
                    out bytesRead,
                    _disposeCts.Token
                );
                if (bytesRead > 0)
                {
                    _bulkReadBuffer.AsSpan(0, bytesRead).CopyTo(destination);
                }
                return result;
            }
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogWarning(
                ex,
                "BulkRead interrupted. {ErrorType}: {ErrorMessage}",
                ex.GetType().Name,
                ex.Message
            );
            bytesRead = 0;
            return LibUsbResult.Interrupted;
        }
        finally
        {
            _disposeLock.ExitReadLock();
        }
    }

    public LibUsbResult BulkWrite(byte[] source, int count, out int bytesWritten, int timeout) =>
        BulkWrite(source.AsSpan(), count, out bytesWritten, timeout);

    public LibUsbResult BulkWrite(ReadOnlySpan<byte> source, out int bytesWritten, int timeout) =>
        BulkWrite(source, source.Length, out bytesWritten, timeout);

    /// <inheritdoc />
    public LibUsbResult BulkWrite(
        ReadOnlySpan<byte> source,
        int count,
        out int bytesWritten,
        int timeout
    )
    {
        if (count > source.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                count,
                "Count must not be greater than the provided buffer length."
            );
        }
        CheckTransferTimeout(timeout);
        _disposeLock.EnterReadLock(); // Use read lock for reads and writes, to support duplex
        try
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UsbInterface));
            }
            var bufferLength = Math.Min(count, WriteBufferSize);
            lock (_bulkWriteLock)
            {
                source[..bufferLength].CopyTo(_bulkWriteBuffer.AsSpan(0, bufferLength));
                return LibUsbTransfer.ExecuteSync(
                    _logger,
                    _device.Handle,
                    LibUsbTransferType.Bulk,
                    _writeEndpoint.Value.EndpointAddress,
                    _bulkWriteBufferHandle,
                    bufferLength,
                    timeout > 0 ? (uint)timeout : 0,
                    out bytesWritten,
                    _disposeCts.Token
                );
            }
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogWarning(
                ex,
                "BulkWrite interrupted. {ErrorType}: {ErrorMessage}",
                ex.GetType().Name,
                ex.Message
            );
            bytesWritten = 0;
            return LibUsbResult.Interrupted;
        }
        finally
        {
            _disposeLock.ExitReadLock();
        }
    }

    public override string ToString() =>
        $"{_descriptor.InterfaceClass} #{_descriptor.InterfaceNumber}";

    private IUsbEndpointDescriptor GetEndpoint(
        IUsbInterfaceDescriptor descriptor,
        UsbEndpointDirection direction
    )
    {
        var endpoint = descriptor.GetEndpoint(direction, out var count);
        if (count > 1)
        {
            _logger.LogWarning(
                "Interface #{InterfaceNumber} has {EndpointCount} {EndpointDirection} endpoints."
                    + "The first endpoint was selected.",
                descriptor.InterfaceNumber,
                count,
                direction
            );
        }
        return endpoint;
    }

    /// <summary>
    /// Throw ArgumentException when timeout is 0 or less than -1.
    /// </summary>
    private static void CheckTransferTimeout(int timeout)
    {
        if (timeout is 0 or < -1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                timeout,
                "Invalid timeout; must be greater than zero or -1 (infinite)."
            );
        }
    }

    /// <summary>
    /// Disposes this interface and associated resources. Ongoing read and write
    /// transfers are canceled and allocated read and write memory buffers are freed.
    /// </summary>
    public void Dispose()
    {
        lock (_disposeCts)
        {
            if (_disposed)
            {
                _logger.LogDebug("USB interface {UsbInterface} already disposed.", this);
                return;
            }
            // Prevent new transfers from starting and cancel any ongoing
            _disposeCts.Cancel();
            _disposeLock.EnterWriteLock();
            try
            {
                // Ask UsbDevice to close interface and remove it from list of open interfaces
                _device.ReleaseInterface(_descriptor.InterfaceNumber);
                // Free read and write buffers
                _bulkReadBufferHandle.Free();
                _bulkWriteBufferHandle.Free();
                _disposeCts.Dispose();
                _disposed = true;
            }
            finally
            {
                _disposeLock.ExitWriteLock();
            }
        }
    }
}
