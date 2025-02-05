using Moq;
using System.Net.Sockets;
using IZTechTask;
using Xunit;

//TODO TCP Client mock is not working - tests are not running
public class NetSdrClientTests
{
    private readonly Mock<TcpClient?> _mockTcpClient;
    private readonly Mock<NetworkStream> _mockStream;
    private readonly NetSdrClient _client;

    public NetSdrClientTests()
    {
        _mockTcpClient = new Mock<TcpClient?>();
        _mockStream = new Mock<NetworkStream>();
        _mockTcpClient.Setup(x => x.GetStream()).Returns(_mockStream.Object);

        _client = new NetSdrClient(_mockTcpClient.Object);
    }

    [Fact]
    public void Connect_ShouldCallConnectMethod()
    {
        // Arrange
        _mockTcpClient.Setup(tcp => tcp.Connect(It.IsAny<string>(), It.IsAny<int>())).Verifiable();

        // Act
        _client.Connect("localhost", 50000);

        // Assert
        _mockTcpClient.Verify(tcp => tcp.Connect("localhost", 50000), Times.Once);
    }

    [Fact]
    public void Disconnect_ShouldCallDispose()
    {
        // Arrange
        _mockTcpClient.Setup(tcp => tcp.Dispose()).Verifiable();

        // Act
        _client.Disconnect();

        // Assert
        _mockTcpClient.Verify(tcp => tcp.Dispose(), Times.Once);
    }
}