using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.ObjectPool;
using NostrSure.Infrastructure.Client.Implementation;
using NostrSure.Infrastructure.Client.Abstractions;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NostrSure.Tests.Client.Connection;

[TestClass]

[TestCategory("Connection - MessageReceiver")]
public class MessageReceiverTests
{
    private ClientWebSocket _webSocket = null!;
    private TestErrorHandler _errorHandler = null!;
    private ConnectionStateManager _stateManager = null!;
    private ObjectPool<StringBuilder> _stringBuilderPool = null!;
    private MessageReceiver _receiver = null!;

    [TestInitialize]
    public void Setup()
    {
        _webSocket = new ClientWebSocket();
        _errorHandler = new TestErrorHandler();
        _stateManager = new ConnectionStateManager();
        var poolProvider = new DefaultObjectPoolProvider();
        _stringBuilderPool = poolProvider.Create(new StringBuilderPooledObjectPolicy());
        _receiver = new MessageReceiver(_webSocket, _errorHandler, _stateManager, _stringBuilderPool);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task ReceiveAsync_ThrowsIfNotConnected()
    {
        await _receiver.ReceiveAsync();
    }

    [TestMethod]
    public async Task Dispose_CancelsAndDisposes()
    {
        _receiver.Dispose();
        await Task.CompletedTask;
    }

    [TestMethod]
    public async Task StartReceivingAsync_WarningIfAlreadyStarted()
    {
        await _receiver.StartReceivingAsync();
        await _receiver.StartReceivingAsync();
    }

    [TestMethod]
    public async Task StopReceivingAsync_CancelsLoopGracefully()
    {
        await _receiver.StartReceivingAsync();
        await _receiver.StopReceivingAsync();
    }

    private class TestErrorHandler : IConnectionErrorHandler
    {
        public event EventHandler<Exception>? ErrorOccurred;
        public Task HandleErrorAsync(Exception exception, string context)
        {
            ErrorOccurred?.Invoke(this, exception);
            return Task.CompletedTask;
        }
        public bool ShouldReconnect(Exception exception) => false;
    }
}
