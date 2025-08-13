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
    private readonly nint _deviceHandle;
    private readonly IUsbInterfaceDescriptor _descriptor;
    private readonly byte[] _bulkReadBuffer;
    private readonly GCHandle _bulkReadBufferHandle;
    private readonly Lazy<IUsbEndpointDescriptor> _readEndpoint;
    private readonly ReaderWriterLockSlim _bulkReadLock = new();
    private readonly byte[] _bulkWriteBuffer;
    private readonly GCHandle _bulkWriteBufferHandle;
    private readonly Lazy<IUsbEndpointDescriptor> _writeEndpoint;
    private readonly ReaderWriterLockSlim _bulkWriteLock = new();
    private readonly ReaderWriterLockSlim _disposeLock = new();
    private LibUsbTransfer? _lastReadTransfer;
    private LibUsbTransfer? _lastWriteTransfer;
    private bool _disposing;
    private bool _disposed;

    /// <inheritdoc />
    public byte Number => _descriptor.InterfaceNumber;

    /// <summary>
    /// A type representing a claimed USB interface.
    /// </summary>
    /// <param name="loggerFactory">An optional logger factory.</param>
    /// <param name="deviceHandle">The parent USB device handle.</param>
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
        nint deviceHandle,
        IUsbInterfaceDescriptor descriptor,
        IUsbEndpointDescriptor? readEndpoint = default,
        IUsbEndpointDescriptor? writeEndpoint = default
    )
    {
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<UsbInterface>();
        _deviceHandle = deviceHandle;
        _descriptor = descriptor;
        _bulkReadBuffer = new byte[ReadBufferSize];
        _bulkReadBufferHandle = GCHandle.Alloc(_bulkReadBuffer, GCHandleType.Pinned);
        _readEndpoint = readEndpoint is null
            ? new Lazy<IUsbEndpointDescriptor>(GetEndpoint(descriptor, UsbEndpointDirection.Input))
            : new Lazy<IUsbEndpointDescriptor>(readEndpoint);
        _bulkWriteBuffer = new byte[WriteBufferSize];
        _bulkWriteBufferHandle = GCHandle.Alloc(_bulkWriteBuffer, GCHandleType.Pinned);
        _writeEndpoint = writeEndpoint is null
            ? new Lazy<IUsbEndpointDescriptor>(GetEndpoint(descriptor, UsbEndpointDirection.Output))
            : new Lazy<IUsbEndpointDescriptor>(writeEndpoint);
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

    //public LibUsbError Read(ReadOnlySpan<byte> buffer, int timeout, out int transferLength) { }

    /// <inheritdoc />
    public LibUsbResult BulkRead(byte[] buffer, out int bytesRead, int timeout = Timeout.Infinite)
    {
        CheckTransferTimeout(timeout);
        _disposeLock.EnterReadLock(); // Use read lock for reads and writes, to support duplex
        try
        {
            CheckDisposed();
            var bufferLength = Math.Min(buffer.Length, ReadBufferSize);
            lock (_bulkReadLock)
            {
                var result = Transfer(
                    _readEndpoint.Value.EndpointAddress,
                    _bulkReadBufferHandle,
                    bufferLength,
                    timeout > 0 ? (uint)timeout : 0,
                    out _lastReadTransfer,
                    out bytesRead
                );
                Array.Copy(_bulkReadBuffer, buffer, bytesRead);
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

    //public LibUsbError Write(ReadOnlySpan<byte> buffer, int timeout, out int transferLength) { }

    /// <inheritdoc />
    public LibUsbResult BulkWrite(byte[] buffer, int count, out int bytesWritten, int timeout)
    {
        if (count > buffer.Length)
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
            CheckDisposed();
            var bufferLength = Math.Min(count, WriteBufferSize);
            lock (_bulkWriteLock)
            {
                Array.Copy(buffer, _bulkWriteBuffer, bufferLength);
                return Transfer(
                    _writeEndpoint.Value.EndpointAddress,
                    _bulkWriteBufferHandle,
                    bufferLength,
                    timeout > 0 ? (uint)timeout : 0,
                    out _lastWriteTransfer,
                    out bytesWritten
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

    private LibUsbResult Transfer(
        byte endpointAddress,
        GCHandle bufferHandle,
        int bufferLength,
        uint timeout,
        out LibUsbTransfer submittedTransfer,
        out int bytesTransferred,
        LibUsbTransferType transferType = LibUsbTransferType.Bulk
    )
    {
        // Do not start any new transfers after interface dispose has been called
        if (_disposing)
        {
            throw new ObjectDisposedException(nameof(UsbInterface), " USB interface is disposing.");
        }

        // Create a reset event for the transfer callback
        var transferCompleteEvent = new ManualResetEvent(false);
        var transferStatus = LibUsbTransferStatus.Error;
        var transferLength = 0;

        // Create a transfer with a completion handler
        using var transfer = new LibUsbTransfer(
            _deviceHandle,
            endpointAddress,
            bufferHandle,
            bufferLength,
            transferType,
            timeout,
            (transfer, status, length) =>
            {
                transferStatus = status;
                transferLength = length;
#if DEBUG
                _logger.LogTrace(
                    "Transfer '{TransferStatus}' after {TransferLength} of {BufferLength} bytes.",
                    transferStatus,
                    transferLength,
                    bufferLength
                );
#endif
                // Signal transfer completion
                _ = transferCompleteEvent.Set();
            }
        );

        // Store the submittedTransfer to allow cancellation on dispose; then submit
        submittedTransfer = transfer;
        var transferResult = transfer.Submit();
        if (transferResult is not LibUsbResult.Success)
        {
            bytesTransferred = 0;
            return transferResult;
        }

        // We should not dispose the transfer if there is still a chance that
        // the callback is triggered, doing so may cause writes to freed memory.
        // Hence, we wait indefinitely for completion or cancellation.
        _ = transferCompleteEvent.WaitOne();

        // The transfer is complete, cancelled or failed; return result
        bytesTransferred = transferLength;
        return transferStatus.ToLibUsbError();
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
    /// Throw ObjectDisposedException when UsbDevice is disposed.
    /// </summary>
    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(UsbInterface));
        }
    }

    public void Dispose()
    {
        // Prevent new transfers from starting and cancel any ongoing
        _disposing = true;
        _ = _lastReadTransfer?.Cancel();
        _ = _lastWriteTransfer?.Cancel();
        _disposeLock.EnterWriteLock();
        try
        {
            if (_disposed)
            {
                _logger.LogDebug("USB interface {UsbInterface} already disposed.", ToString());
                return;
            }
            _bulkReadBufferHandle.Free();
            _bulkWriteBufferHandle.Free();
            _disposed = true;
        }
        finally
        {
            _disposeLock.ExitWriteLock();
        }
    }
}
