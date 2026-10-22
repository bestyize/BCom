using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BCom.CrossPlatform;

/// <summary>
/// BCOMCAP1: 8-byte magic, ushort port UTF-8 byte count, port name; repeated
/// records: Int64 UTC ticks, byte direction (0 RX, 1 TX), Int32 length, bytes.
/// One file represents a single port. Rotated parts are independently replayable.
/// </summary>
public sealed class SerialCapture : IAsyncDisposable
{
    private static readonly byte[] Magic = "BCOMCAP1"u8.ToArray();
    private readonly Channel<SerialTraceEvent> _queue;
    private readonly string _path;
    private readonly string _port;
    private readonly long _maxFileBytes;
    private readonly Task _writerTask;
    private long _droppedBytes;
    private long _writtenBytes;
    private int _closed;
    private Exception? _writeError;

    public SerialCapture(string path, string port, int maxPendingEvents = 256,
        long maxFileBytes = 64L * 1024 * 1024)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Capture path is required.", nameof(path));
        if (maxPendingEvents < 1) throw new ArgumentOutOfRangeException(nameof(maxPendingEvents));
        if (maxFileBytes < 4096) throw new ArgumentOutOfRangeException(nameof(maxFileBytes));
        if (Encoding.UTF8.GetByteCount(port) > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(port));
        _path = path;
        _port = port;
        _maxFileBytes = maxFileBytes;
        _queue = Channel.CreateBounded<SerialTraceEvent>(new BoundedChannelOptions(maxPendingEvents)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
        _writerTask = Task.Run(WriteLoopAsync);
    }

    public long DroppedBytes => Interlocked.Read(ref _droppedBytes);
    public long WrittenBytes => Interlocked.Read(ref _writtenBytes);
    public Exception? WriteError => _writeError;

    public bool TryRecord(DateTimeOffset timestamp, SerialTraceDirection direction, byte[] data)
    {
        if (data.Length == 0) return true;
        if (Volatile.Read(ref _closed) != 0 || _writeError != null)
        {
            Interlocked.Add(ref _droppedBytes, data.Length);
            return false;
        }
        // Make a snapshot; SerialService hands ownership to callers today, but
        // recorders should never depend on that aliasing contract.
        var record = new SerialTraceEvent(timestamp, _port, direction, (byte[])data.Clone());
        if (record.Data.Length > _maxFileBytes - 256 || !_queue.Writer.TryWrite(record))
        {
            Interlocked.Add(ref _droppedBytes, data.Length);
            return false;
        }
        return true;
    }

    private async Task WriteLoopAsync()
    {
        FileStream? stream = null;
        int part = 0;
        long size = 0;
        try
        {
            await foreach (var e in _queue.Reader.ReadAllAsync())
            {
                int recordSize = checked(13 + e.Data.Length);
                if (stream == null || size + recordSize > _maxFileBytes)
                {
                    if (stream != null) await stream.DisposeAsync();
                    string file = part == 0 ? _path : _path + ".part" + part.ToString("D3");
                    part++;
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
                    stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write,
                        FileShare.Read, bufferSize: 64 * 1024, useAsync: true);
                    var port = Encoding.UTF8.GetBytes(_port);
                    var header = new byte[10 + port.Length];
                    Magic.CopyTo(header, 0);
                    BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(8), checked((ushort)port.Length));
                    port.CopyTo(header, 10);
                    await stream.WriteAsync(header);
                    size = header.Length;
                }
                var info = new byte[13];
                BinaryPrimitives.WriteInt64LittleEndian(info.AsSpan(0), e.Timestamp.UtcDateTime.Ticks);
                info[8] = checked((byte)e.Direction);
                BinaryPrimitives.WriteInt32LittleEndian(info.AsSpan(9), e.Data.Length);
                await stream.WriteAsync(info);
                await stream.WriteAsync(e.Data);
                size += recordSize;
                Interlocked.Add(ref _writtenBytes, e.Data.Length);
            }
        }
        catch (Exception ex)
        {
            _writeError = ex;
        }
        finally
        {
            if (stream != null) await stream.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _closed, 1) == 0)
            _queue.Writer.TryComplete();
        await _writerTask.ConfigureAwait(false);
        if (_writeError != null) throw new IOException("Serial capture failed.", _writeError);
    }

    public static async IAsyncEnumerable<SerialTraceEvent> ReplayAsync(string file,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token = default)
    {
        await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read,
            FileShare.Read, bufferSize: 64 * 1024, useAsync: true);
        byte[] header = new byte[10];
        await stream.ReadExactlyAsync(header, token);
        if (!header.AsSpan(0, 8).SequenceEqual(Magic))
            throw new InvalidDataException("Not a BCOMCAP1 capture.");
        int portLength = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(8));
        var portBytes = new byte[portLength];
        await stream.ReadExactlyAsync(portBytes, token);
        string port = Encoding.UTF8.GetString(portBytes);
        var info = new byte[13];
        while (stream.Position < stream.Length)
        {
            await stream.ReadExactlyAsync(info, token);
            long ticks = BinaryPrimitives.ReadInt64LittleEndian(info.AsSpan(0));
            if (info[8] > 1) throw new InvalidDataException("Invalid capture direction.");
            int length = BinaryPrimitives.ReadInt32LittleEndian(info.AsSpan(9));
            if (length < 0 || length > 64 * 1024 * 1024)
                throw new InvalidDataException("Invalid capture record size.");
            var data = new byte[length];
            await stream.ReadExactlyAsync(data, token);
            yield return new SerialTraceEvent(new DateTimeOffset(ticks, TimeSpan.Zero),
                port, (SerialTraceDirection)info[8], data);
        }
    }
}
