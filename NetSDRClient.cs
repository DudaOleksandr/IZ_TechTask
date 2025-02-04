using System.Buffers.Binary;
using System.Net.Sockets;
using IZTechTask.Enums;
using IZTechTask.Exceptions;

namespace IZTechTask;

public class NetSdrClient : IDisposable
{
    private TcpClient _tcpClient;
    private NetworkStream _stream;
    
    private const ushort ReceiverStateCode = 0x0018;
    private const ushort ReceiverFrequencyCode = 0x0020;
    
    private bool IsConnected => _tcpClient?.Connected ?? false;
    
    public void Connect(string host, int port = 50000)
    {
        Disconnect();
        _tcpClient = new TcpClient();
        _tcpClient.Connect(host, port);
        _stream = _tcpClient.GetStream();
    }

    public void Disconnect()
    {
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

        throw new NotImplementedException();
        
        HandleResponse(controlCode);
    }

    private void HandleResponse(ushort expectedControlCode)
    {
        throw new NotImplementedException();
    }

    private void EnsureConnected()
    {
        if (!IsConnected) throw new NetSdrException("Not connected");
    }

    public void Dispose() => Disconnect();
}

