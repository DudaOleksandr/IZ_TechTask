using System.Net.Sockets;

namespace IZTechTask;

public class IqDataReceiver : IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly BinaryWriter _binaryWriter;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly int _udpPort = 60000;

    public IqDataReceiver(string outputFilePath)
    {
        _udpClient = new UdpClient(_udpPort);
        _binaryWriter = new BinaryWriter(File.Open(outputFilePath, FileMode.Create, FileAccess.Write));
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task StartReceivingAsync()
    {
        Console.WriteLine($"Starting UDP listener on port {_udpPort}...");

        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var udpResult = await _udpClient.ReceiveAsync().WithCancellation(_cancellationTokenSource.Token);
                
                _binaryWriter.Write(udpResult.Buffer);

                Console.WriteLine($"Received {udpResult.Buffer.Length} bytes.");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("UDP listener stopped.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in UDP listener: {ex.Message}");
        }
    }

    public void StopReceiving()
    {
        _cancellationTokenSource.Cancel();
    }

    public void Dispose()
    {
        StopReceiving();
        _udpClient?.Dispose();
        _binaryWriter?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}

public static class UdpClientExtensions
{
    public static async Task<UdpReceiveResult> WithCancellation(this Task<UdpReceiveResult> task, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();
        using (cancellationToken.Register(() => tcs.TrySetResult(true)))
        {
            if (task != await Task.WhenAny(task, tcs.Task))
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }
        return await task;
    }
}