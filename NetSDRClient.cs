using System.Buffers.Binary;
using System.Net.Sockets;
using IZTechTask.Enums;
using IZTechTask.Exceptions;

namespace IZTechTask;

public class NetSdrClient : IDisposable
{
    public event Action<ushort, byte[]> OnUnsolicitedControlItem;
    
    private TcpClient _tcpClient;
    private NetworkStream _stream;
    private IqDataReceiver _iqDataReceiver;
    
    private const ushort ReceiverStateCode = 0x0018;
    private const ushort ReceiverFrequencyCode = 0x0020;
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    public NetSdrClient(TcpClient? tcpClient = null)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _tcpClient = tcpClient ?? new TcpClient();
    }
    
    private bool IsConnected => _tcpClient?.Connected ?? false;
    
    public void Connect(string host, int port = 50000)
    {
        Disconnect();
        _tcpClient.Connect(host, port);
        _stream = _tcpClient.GetStream();
        
        _ = StartListeningAsync(_cancellationTokenSource.Token);
    }

    public void Disconnect()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _stream?.Dispose();
        _tcpClient?.Dispose();
    }

    // 4.2.1 Receiver State
    public void StartIqStream(DataFormat format, CaptureMode mode, byte fifoBlocks = 0)
    {
        EnsureConnected();
        
        SendControlItem(
            controlCode: ReceiverStateCode,
            parameters:
            [
                (byte)format,
                (byte)StreamState.Run,
                (byte)mode,
                mode == CaptureMode.Fifo16Bit ? fifoBlocks : (byte)0x00
            ]
        );
    }

    public void StopIqStream()
    {
        EnsureConnected();
        
        SendControlItem(
            controlCode: ReceiverStateCode,
            parameters:
            [
                (byte)DataFormat.Real,
                (byte)StreamState.Stop,
                (byte)CaptureMode.Contiguous16Bit,
                0x00
            ]
        );
    }
    
    public void StartIqDataReceiver(string outputFilePath)
    {
        _iqDataReceiver = new IqDataReceiver(outputFilePath);
        _ = _iqDataReceiver.StartReceivingAsync();
    }
    
    public void StopIqDataReceiver()
    {
        _iqDataReceiver?.StopReceiving();
        _iqDataReceiver?.Dispose();
    }

    // 4.2.3 Receiver Frequency
    public void SetFrequency(ReceiverChannel channel, ulong frequencyHz)
    {
        Span<byte> frequencyBytes = stackalloc byte[5];
        BinaryPrimitives.WriteUInt64LittleEndian(frequencyBytes, frequencyHz);
        
        SendControlItem(
            controlCode: ReceiverFrequencyCode,
            parameters: new[] { (byte)channel }
                .Concat(frequencyBytes.ToArray())
                .ToArray()
        );
    }

    private void SendControlItem(ushort controlCode, byte[] parameters)
    {
        EnsureConnected();
        
        var totalLength = 4 + (parameters?.Length ?? 0); // Заголовок (4 байти) + параметри
        if (totalLength > 8194)
            throw new NetSdrException("Message length exceeds maximum allowed size.");
        
        Span<byte> message = stackalloc byte[totalLength];
        
        var lengthField = (ushort)(totalLength - 2);
        var headerValue = (ushort)((lengthField & 0x1FFF) | ((ushort)MessageType.SetControlItem << 13));
        
        BinaryPrimitives.WriteUInt16LittleEndian(message, headerValue);
        BinaryPrimitives.WriteUInt16LittleEndian(message[2..], controlCode);
        parameters?.AsSpan().CopyTo(message[4..]);
        
        _stream.Write(message);
        HandleResponse(controlCode);
    }

    private void HandleResponse(ushort expectedControlCode)
    {
        Span<byte> header = stackalloc byte[4];
        _stream.ReadExactly(header);
        
        var length = BinaryPrimitives.ReadUInt16LittleEndian(header);
        var typeField = (byte)((header[1] >> 5) & 0x07);
        var controlCode = BinaryPrimitives.ReadUInt16LittleEndian(header[2..]);

        // NAK
        if (length == 2 && (MessageType)typeField == MessageType.ResponseToSetOrRequest)
        {
            throw new NetSdrException("NAK received: Control Item not supported.");
        }

        // Unsolicited Control Item
        if ((MessageType)typeField == MessageType.UnsolicitedControlItem)
        {
            Span<byte> body = stackalloc byte[length - 4];
            _stream.ReadExactly(body);
            OnUnsolicitedControlItem?.Invoke(controlCode, body.ToArray());
            return;
        }
        
        if (controlCode != expectedControlCode)
            throw new NetSdrException($"Unexpected response code: 0x{controlCode:X4}");

        // ACK
        if (length > 4)
        {
            Span<byte> body = stackalloc byte[length - 4];
            _stream.ReadExactly(body);
        }
    }
    private void EnsureConnected()
    {
        if (!IsConnected) throw new NetSdrException("Not connected");
    }
    
    private async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        var headerBuffer = new byte[4];

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _stream.ReadExactlyAsync(headerBuffer, cancellationToken);
                
                var length = BinaryPrimitives.ReadUInt16LittleEndian(headerBuffer);
                var type = (byte)((headerBuffer[1] >> 5) & 0x07);
                var controlCode = BinaryPrimitives.ReadUInt16LittleEndian(headerBuffer.AsSpan(2));

                if (type == (byte)MessageType.UnsolicitedControlItem)
                {
                    var bodyBuffer = new byte[length - 4];
                    await _stream.ReadExactlyAsync(bodyBuffer, cancellationToken);
                    
                    OnUnsolicitedControlItem?.Invoke(controlCode, bodyBuffer);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in StartListeningAsync: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        StopIqDataReceiver();
        Disconnect();
    }
}

